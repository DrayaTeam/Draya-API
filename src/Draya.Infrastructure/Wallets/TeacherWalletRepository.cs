using Draya.Domain.Wallets;
using Draya.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Draya.Infrastructure.Wallets;

public class TeacherWalletRepository : ITeacherWalletRepository
{
    private readonly ApplicationDbContext _context;

    public TeacherWalletRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<TeacherWallet?> GetByTeacherIdAsync(Guid teacherId, CancellationToken cancellationToken = default)
        => await _context.TeacherWallets.FirstOrDefaultAsync(w => w.TeacherId == teacherId, cancellationToken);

    public async Task AddAsync(TeacherWallet wallet, CancellationToken cancellationToken = default)
        => await _context.TeacherWallets.AddAsync(wallet, cancellationToken);

    public Task UpdateAsync(TeacherWallet wallet, CancellationToken cancellationToken = default)
    {
        _context.TeacherWallets.Update(wallet);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => await _context.SaveChangesAsync(cancellationToken);
}
