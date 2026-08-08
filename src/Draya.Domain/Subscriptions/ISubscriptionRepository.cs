namespace Draya.Domain.Subscriptions;

public interface ISubscriptionRepository
{
    Task<TeacherSubscription?> GetActiveForTeacherAsync(Guid teacherId, CancellationToken cancellationToken = default);
    Task<UsageCounter?> GetUsageForTeacherAsync(Guid teacherId, DateOnly periodMonth, CancellationToken cancellationToken = default);

    // Write operations to allow creating subscriptions and saving them in the shared DbContext
    Task AddAsync(TeacherSubscription subscription, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
