using Draya.Domain.Wallets;
using Draya.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Draya.Infrastructure.Wallets;

public class WalletTransactionRepository : IWalletTransactionRepository
{
    private readonly ApplicationDbContext _context;

    public WalletTransactionRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(WalletTransaction transaction, CancellationToken cancellationToken = default)
    {
        await _context.WalletTransactions.AddAsync(transaction, cancellationToken);
    }

    public async Task<(List<WalletTransaction> Items, int TotalCount)> GetPaginatedByTeacherIdAsync(
        Guid teacherId, 
        int pageNumber, 
        int pageSize, 
        CancellationToken cancellationToken = default)
    {
        var query = _context.WalletTransactions
            .AsNoTracking()
            .Where(t => t.TeacherId == teacherId);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
