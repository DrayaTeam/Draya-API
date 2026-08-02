using Draya.Domain.Subscriptions;
using Draya.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
namespace Draya.Infrastructure.Subscriptions;
public class SubscriptionRepository(ApplicationDbContext context) : ISubscriptionRepository { public Task<TeacherSubscription?> GetActiveForTeacherAsync(Guid teacherId, CancellationToken ct = default) => context.TeacherSubscriptions.AsNoTracking().Include(x => x.Plan).Where(x => x.TeacherId == teacherId && x.Status == SubscriptionStatus.Active).OrderByDescending(x => x.StartDate).FirstOrDefaultAsync(ct); public Task<UsageCounter?> GetUsageForTeacherAsync(Guid teacherId, DateOnly period, CancellationToken ct = default) => context.UsageCounters.AsNoTracking().FirstOrDefaultAsync(x => x.TeacherId == teacherId && x.PeriodMonth == period, ct); }
