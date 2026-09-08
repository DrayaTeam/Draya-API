namespace Draya.Domain.Notifications;

public interface INotificationRepository
{
    Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Notification>> GetByUserIdAsync(Guid userId, int page, int pageSize, bool unreadOnly, CancellationToken cancellationToken = default);
    Task<int> GetTotalCountAsync(Guid userId, bool unreadOnly, CancellationToken cancellationToken = default);
    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(Notification notification, CancellationToken cancellationToken = default);
    Task UpdateAsync(Notification notification, CancellationToken cancellationToken = default);
    Task DeleteAsync(Notification notification, CancellationToken cancellationToken = default);
    Task MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default);
    Task ClearAllAsync(Guid userId, CancellationToken cancellationToken = default);
}
