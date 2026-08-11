using Draya.Domain.Wallets;
using Draya.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Draya.Infrastructure.Wallets;

public class TeacherPayoutAccountRepository : ITeacherPayoutAccountRepository
{
    private readonly ApplicationDbContext _context;

    public TeacherPayoutAccountRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(TeacherPayoutAccount account, CancellationToken cancellationToken = default)
    {
        await _context.TeacherPayoutAccounts.AddAsync(account, cancellationToken);
    }

    public async Task<TeacherPayoutAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.TeacherPayoutAccounts.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<List<TeacherPayoutAccount>> GetByTeacherIdAsync(Guid teacherId, CancellationToken cancellationToken = default)
    {
        return await _context.TeacherPayoutAccounts
            .AsNoTracking()
            .Where(a => a.TeacherId == teacherId)
            .OrderByDescending(a => a.IsDefault)
            .ThenByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<TeacherPayoutAccount?> GetDefaultByTeacherIdAsync(Guid teacherId, CancellationToken cancellationToken = default)
    {
        return await _context.TeacherPayoutAccounts
            .FirstOrDefaultAsync(a => a.TeacherId == teacherId && a.IsDefault, cancellationToken);
    }

    public Task UpdateAsync(TeacherPayoutAccount account, CancellationToken cancellationToken = default)
    {
        _context.TeacherPayoutAccounts.Update(account);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(TeacherPayoutAccount account, CancellationToken cancellationToken = default)
    {
        _context.TeacherPayoutAccounts.Remove(account);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
