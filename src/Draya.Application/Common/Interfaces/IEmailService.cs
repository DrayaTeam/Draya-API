namespace Draya.Application.Common.Interfaces;

public interface IEmailService
{
    Task SendPasswordResetEmailAsync(string recipientEmail, string resetToken, CancellationToken cancellationToken = default);
}
