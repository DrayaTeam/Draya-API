namespace Draya.Domain.Subscriptions;

public interface IUsageCounterRepository
{
    Task<UsageCounter?> GetForTeacherAsync(Guid teacherId, DateOnly periodMonth, CancellationToken cancellationToken = default);
    Task AddAsync(UsageCounter usageCounter, CancellationToken cancellationToken = default);
    Task UpdateAsync(UsageCounter usageCounter, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
