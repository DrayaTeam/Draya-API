using Draya.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;

namespace Draya.Infrastructure.Identity.Services;

public class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _configuration;

    public SmtpEmailService(IConfiguration configuration) => _configuration = configuration;

    public async Task SendPasswordResetEmailAsync(string recipientEmail, string resetToken, CancellationToken cancellationToken = default)
    {
        var settings = _configuration.GetSection("Email");
        var host = settings["Host"] ?? throw new InvalidOperationException("Email host is not configured.");
        var from = settings["From"] ?? throw new InvalidOperationException("Email sender is not configured.");
        var resetUrl = settings["PasswordResetUrl"] ?? throw new InvalidOperationException("Password reset URL is not configured.");
        var port = settings.GetValue<int?>("Port") ?? 587;
        var userName = settings["UserName"];
        var password = settings["Password"];
        var separator = resetUrl.Contains('?') ? "&" : "?";
        var link = $"{resetUrl}{separator}token={Uri.EscapeDataString(resetToken)}";

        using var message = new MailMessage(from, recipientEmail)
        {
            Subject = "Reset your Draya password",
            Body = $"Use this link to reset your password: {link}",
            IsBodyHtml = false
        };
        using var client = new SmtpClient(host, port) { EnableSsl = true };
        if (!string.IsNullOrWhiteSpace(userName) && password is not null)
            client.Credentials = new NetworkCredential(userName, password);

        await client.SendMailAsync(message, cancellationToken);
    }

    public async Task SendSupervisorInviteEmailAsync(string recipientEmail, string inviteToken, CancellationToken cancellationToken = default)
    {
        var settings = _configuration.GetSection("Email");
        var host = settings["Host"] ?? throw new InvalidOperationException("Email host is not configured.");
        var from = settings["From"] ?? throw new InvalidOperationException("Email sender is not configured.");
        var inviteUrl = settings["SupervisorInviteUrl"] ?? "https://draya.com/auth/accept-invite";
        var port = settings.GetValue<int?>("Port") ?? 587;
        var userName = settings["UserName"];
        var password = settings["Password"];
        
        var separator = inviteUrl.Contains('?') ? "&" : "?";
        var link = $"{inviteUrl}{separator}token={Uri.EscapeDataString(inviteToken)}&email={Uri.EscapeDataString(recipientEmail)}";

        using var message = new MailMessage(from, recipientEmail)
        {
            Subject = "Welcome to Draya - You have been invited as a Supervisor",
            Body = $"You have been invited to join Draya as a Supervisor. Use this link to set your password and accept the invitation: {link}",
            IsBodyHtml = false
        };
        
        using var client = new SmtpClient(host, port) { EnableSsl = true };
        if (!string.IsNullOrWhiteSpace(userName) && password is not null)
            client.Credentials = new NetworkCredential(userName, password);

        await client.SendMailAsync(message, cancellationToken);
    }
}
