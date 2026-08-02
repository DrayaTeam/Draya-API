namespace Draya.Domain.Subscriptions;
public interface ISubscriptionRepository { Task<TeacherSubscription?> GetActiveForTeacherAsync(Guid teacherId, CancellationToken cancellationToken = default); Task<UsageCounter?> GetUsageForTeacherAsync(Guid teacherId, DateOnly periodMonth, CancellationToken cancellationToken = default); }
