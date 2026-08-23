using Draya.Domain.Notifications;
using Draya.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Draya.Infrastructure.Notifications;

public class NotificationRepository : INotificationRepository
{
    private readonly ApplicationDbContext _context;

    public NotificationRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Notifications.FirstOrDefaultAsync(n => n.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Notification>> GetByUserIdAsync(Guid userId, int page, int pageSize, bool unreadOnly, CancellationToken cancellationToken = default)
    {
        var query = _context.Notifications.Where(n => n.UserId == userId);

        if (unreadOnly)
        {
            query = query.Where(n => !n.Read);
        }

        return await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetTotalCountAsync(Guid userId, bool unreadOnly, CancellationToken cancellationToken = default)
    {
        var query = _context.Notifications.Where(n => n.UserId == userId);
        
        if (unreadOnly)
        {
            query = query.Where(n => !n.Read);
        }

        return await query.CountAsync(cancellationToken);
    }

    public async Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Notifications.CountAsync(n => n.UserId == userId && !n.Read, cancellationToken);
    }

    public async Task AddAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        await _context.Notifications.AddAsync(notification, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        _context.Notifications.Update(notification);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        _context.Notifications.Remove(notification);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await _context.Notifications
            .Where(n => n.UserId == userId && !n.Read)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.Read, true), cancellationToken);
    }

    public async Task ClearAllAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await _context.Notifications
            .Where(n => n.UserId == userId)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
