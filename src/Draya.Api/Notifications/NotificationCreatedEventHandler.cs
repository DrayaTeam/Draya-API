using Draya.Application.Notifications.Events;
using MediatR;
using Microsoft.AspNetCore.SignalR;

namespace Draya.Api.Notifications;

public class NotificationCreatedEventHandler : INotificationHandler<NotificationCreatedEvent>
{
    private readonly IHubContext<NotificationHub> _hubContext;

    public NotificationCreatedEventHandler(IHubContext<NotificationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task Handle(NotificationCreatedEvent notification, CancellationToken cancellationToken)
    {
        // Broadcast to the specific user's group
        await _hubContext.Clients.Group($"User_{notification.UserId}").SendAsync("ReceiveNotification", new
        {
            id = notification.Notification.Id,
            title = notification.Notification.Title,
            message = notification.Notification.Message,
            type = notification.Notification.Type,
            link = notification.Notification.Link,
            read = notification.Notification.Read,
            createdAt = notification.Notification.CreatedAt
        }, cancellationToken);
    }
}
