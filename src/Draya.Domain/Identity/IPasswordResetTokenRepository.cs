namespace Draya.Domain.Identity;

public interface IPasswordResetTokenRepository
{
    Task<PasswordResetToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task AddAsync(PasswordResetToken passwordResetToken, CancellationToken cancellationToken = default);
    Task UpdateAsync(PasswordResetToken passwordResetToken, CancellationToken cancellationToken = default);
}
