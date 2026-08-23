using Draya.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Mail;

namespace Draya.Infrastructure.Identity.Services;

public class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IConfiguration configuration, ILogger<SmtpEmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendPasswordResetEmailAsync(string recipientEmail, string resetToken, string userRole, CancellationToken cancellationToken = default)
    {
        var settings = _configuration.GetSection("Email");
        var host = settings["Host"] ?? throw new InvalidOperationException("Email host is not configured.");
        var from = settings["From"] ?? throw new InvalidOperationException("Email sender is not configured.");
        
        var html = $@"
        <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; border: 1px solid #e0e0e0; border-radius: 8px; overflow: hidden; color: #333;'>
            <div style='background-color: #1b6d63; color: white; padding: 20px; text-align: center;'>
                <h1 style='margin: 0; font-size: 24px;'>Password Reset Request</h1>
            </div>
            
            <div style='padding: 30px 20px; text-align: center;'>
                <p style='margin-bottom: 20px; font-size: 16px;'>You recently requested to reset your password for your Draya account.</p>
                <p style='margin-bottom: 30px; font-size: 16px;'>Please enter the following 6-digit verification code:</p>
                
                <div style='margin: 30px 0;'>
                    <span style='background-color: #e8f4f2; border: 2px dashed #1b6d63; color: #1b6d63; font-size: 32px; font-weight: bold; padding: 15px 30px; border-radius: 8px; letter-spacing: 5px;'>{resetToken}</span>
                </div>
                
                <p style='color: #666; font-size: 14px; margin-top: 30px;'>If you did not request a password reset, please ignore this email or contact support if you have concerns.</p>
            </div>
            <div style='background-color: #f4f4f4; padding: 15px; text-align: center; font-size: 12px; color: #777;'>
                &copy; Draya Platform. All rights reserved.
            </div>
        </div>";

        using var message = new MailMessage(from, recipientEmail)
        {
            Subject = "Reset your Draya password",
            Body = html,
            IsBodyHtml = true
        };

        var port = settings.GetValue<int?>("Port") ?? 587;
        var userName = settings["UserName"];
        var password = settings["Password"];
        
        using var client = new SmtpClient(host, port) { EnableSsl = true };
        client.UseDefaultCredentials = false;
        if (!string.IsNullOrWhiteSpace(userName) && password is not null)
            client.Credentials = new NetworkCredential(userName, password);

        _logger.LogInformation(
            "Sending password reset email via SMTP host {Host}:{Port} from {From} to {RecipientEmail}. CredentialsConfigured={CredentialsConfigured}",
            host,
            port,
            from,
            recipientEmail,
            !string.IsNullOrWhiteSpace(userName) && !string.IsNullOrWhiteSpace(password));

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
        client.UseDefaultCredentials = false;
        if (!string.IsNullOrWhiteSpace(userName) && password is not null)
            client.Credentials = new NetworkCredential(userName, password);

        await client.SendMailAsync(message, cancellationToken);
    }

    public async Task SendEmailAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        var settings = _configuration.GetSection("Email");
        var host = settings["Host"] ?? throw new InvalidOperationException("Email host is not configured.");
        var from = settings["From"] ?? throw new InvalidOperationException("Email sender is not configured.");
        var port = settings.GetValue<int?>("Port") ?? 587;
        var userName = settings["UserName"];
        var password = settings["Password"];

        using var message = new MailMessage(from, to)
        {
            Subject = subject,
            Body = body,
            IsBodyHtml = true
        };
        
        using var client = new SmtpClient(host, port) { EnableSsl = true };
        client.UseDefaultCredentials = false;
        if (!string.IsNullOrWhiteSpace(userName) && password is not null)
            client.Credentials = new NetworkCredential(userName, password);

        await client.SendMailAsync(message, cancellationToken);
    }
}
