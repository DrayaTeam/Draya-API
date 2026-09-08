using Draya.Domain.Notifications;
using MediatR;

namespace Draya.Application.Notifications.Commands.MarkNotificationAsRead;

public record MarkNotificationAsReadCommand(Guid NotificationId, Guid UserId) : IRequest;

public class MarkNotificationAsReadCommandHandler : IRequestHandler<MarkNotificationAsReadCommand>
{
    private readonly INotificationRepository _notificationRepository;

    public MarkNotificationAsReadCommandHandler(INotificationRepository notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }

    public async Task Handle(MarkNotificationAsReadCommand request, CancellationToken cancellationToken)
    {
        var notification = await _notificationRepository.GetByIdAsync(request.NotificationId, cancellationToken);
        
        if (notification == null || notification.UserId != request.UserId)
        {
            throw new UnauthorizedAccessException("Notification not found or access denied.");
        }

        if (!notification.Read)
        {
            notification.Read = true;
            await _notificationRepository.UpdateAsync(notification, cancellationToken);
        }
    }
}
