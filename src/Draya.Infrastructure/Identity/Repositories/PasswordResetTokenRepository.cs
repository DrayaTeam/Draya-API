using Draya.Domain.Identity;
using Draya.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Draya.Infrastructure.Identity.Repositories;

public class PasswordResetTokenRepository : IPasswordResetTokenRepository
{
    private readonly ApplicationDbContext _context;

    public PasswordResetTokenRepository(ApplicationDbContext context) => _context = context;

    public Task<PasswordResetToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
        => _context.PasswordResetTokens.FirstOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);

    public async Task AddAsync(PasswordResetToken passwordResetToken, CancellationToken cancellationToken = default)
        => await _context.PasswordResetTokens.AddAsync(passwordResetToken, cancellationToken);

    public Task UpdateAsync(PasswordResetToken passwordResetToken, CancellationToken cancellationToken = default)
    {
        _context.PasswordResetTokens.Update(passwordResetToken);
        return Task.CompletedTask;
    }
}
