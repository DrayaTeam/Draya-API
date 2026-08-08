using Draya.Domain.Subscriptions;
using Draya.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Draya.Infrastructure.Subscriptions;

public class SubscriptionRepository : ISubscriptionRepository
{
    private readonly ApplicationDbContext _context;

    public SubscriptionRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<TeacherSubscription?> GetActiveForTeacherAsync(Guid teacherId, CancellationToken ct = default) =>
        _context.TeacherSubscriptions.AsNoTracking().Include(x => x.Plan).Where(x => x.TeacherId == teacherId && x.Status == SubscriptionStatus.Active).OrderByDescending(x => x.StartDate).FirstOrDefaultAsync(ct);

    public Task<UsageCounter?> GetUsageForTeacherAsync(Guid teacherId, DateOnly period, CancellationToken ct = default) =>
        _context.UsageCounters.AsNoTracking().FirstOrDefaultAsync(x => x.TeacherId == teacherId && x.PeriodMonth == period, ct);

    public async Task AddAsync(TeacherSubscription subscription, CancellationToken cancellationToken = default)
    {
        await _context.TeacherSubscriptions.AddAsync(subscription, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}

