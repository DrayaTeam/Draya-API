using Draya.Domain.Wallets;
using Draya.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Draya.Infrastructure.Wallets;

public class WithdrawalRequestRepository : IWithdrawalRequestRepository
{
    private readonly ApplicationDbContext _context;

    public WithdrawalRequestRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(WithdrawalRequest request, CancellationToken cancellationToken = default)
    {
        await _context.WithdrawalRequests.AddAsync(request, cancellationToken);
    }

    public async Task<WithdrawalRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.WithdrawalRequests.FirstOrDefaultAsync(w => w.Id == id, cancellationToken);
    }

    public async Task<(List<WithdrawalRequest> Items, int TotalCount)> GetPaginatedByTeacherIdAsync(
        Guid teacherId, 
        int pageNumber, 
        int pageSize, 
        CancellationToken cancellationToken = default)
    {
        var query = _context.WithdrawalRequests
            .AsNoTracking()
            .Where(w => w.TeacherId == teacherId);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(w => w.RequestedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<(List<WithdrawalRequest> Items, int TotalCount)> GetPaginatedAllAsync(
        WithdrawalStatus? statusFilter, 
        int pageNumber, 
        int pageSize, 
        CancellationToken cancellationToken = default)
    {
        var query = _context.WithdrawalRequests.AsNoTracking();
        if (statusFilter.HasValue)
        {
            query = query.Where(w => w.Status == statusFilter.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(w => w.RequestedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<decimal> GetPendingTotalAmountByTeacherIdAsync(Guid teacherId, CancellationToken cancellationToken = default)
    {
        return await _context.WithdrawalRequests
            .Where(w => w.TeacherId == teacherId && (w.Status == WithdrawalStatus.Pending || w.Status == WithdrawalStatus.Approved))
            .SumAsync(w => w.Amount, cancellationToken);
    }

    public Task UpdateAsync(WithdrawalRequest request, CancellationToken cancellationToken = default)
    {
        _context.WithdrawalRequests.Update(request);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
