using Draya.Domain.Notifications;
using MediatR;

namespace Draya.Application.Notifications.Commands.DeleteNotification;

public record DeleteNotificationCommand(Guid NotificationId, Guid UserId) : IRequest;

public class DeleteNotificationCommandHandler : IRequestHandler<DeleteNotificationCommand>
{
    private readonly INotificationRepository _notificationRepository;

    public DeleteNotificationCommandHandler(INotificationRepository notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }

    public async Task Handle(DeleteNotificationCommand request, CancellationToken cancellationToken)
    {
        var notification = await _notificationRepository.GetByIdAsync(request.NotificationId, cancellationToken);
        
        if (notification == null || notification.UserId != request.UserId)
        {
            throw new UnauthorizedAccessException("Notification not found or access denied.");
        }

        await _notificationRepository.DeleteAsync(notification, cancellationToken);
    }
}
