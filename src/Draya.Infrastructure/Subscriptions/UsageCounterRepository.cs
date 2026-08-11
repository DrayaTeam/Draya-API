using Draya.Domain.Subscriptions;
using Draya.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Draya.Infrastructure.Subscriptions;

public class UsageCounterRepository : IUsageCounterRepository
{
    private readonly ApplicationDbContext _context;

    public UsageCounterRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<UsageCounter?> GetForTeacherAsync(Guid teacherId, DateOnly periodMonth, CancellationToken cancellationToken = default) =>
        _context.UsageCounters.FirstOrDefaultAsync(x => x.TeacherId == teacherId && x.PeriodMonth == periodMonth, cancellationToken);

    public async Task AddAsync(UsageCounter usageCounter, CancellationToken cancellationToken = default)
    {
        await _context.UsageCounters.AddAsync(usageCounter, cancellationToken);
    }

    public Task UpdateAsync(UsageCounter usageCounter, CancellationToken cancellationToken = default)
    {
        _context.UsageCounters.Update(usageCounter);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
