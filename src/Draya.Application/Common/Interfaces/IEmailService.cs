namespace Draya.Application.Common.Interfaces;

public interface IEmailService
{
    Task SendPasswordResetEmailAsync(string recipientEmail, string resetToken, CancellationToken cancellationToken = default);
    Task SendSupervisorInviteEmailAsync(string recipientEmail, string inviteToken, CancellationToken cancellationToken = default);
}
