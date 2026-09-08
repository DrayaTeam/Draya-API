using Draya.Domain.Notifications;
using MediatR;

namespace Draya.Application.Notifications.Commands.MarkAllNotificationsAsRead;

public record MarkAllNotificationsAsReadCommand(Guid UserId) : IRequest;

public class MarkAllNotificationsAsReadCommandHandler : IRequestHandler<MarkAllNotificationsAsReadCommand>
{
    private readonly INotificationRepository _notificationRepository;

    public MarkAllNotificationsAsReadCommandHandler(INotificationRepository notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }

    public async Task Handle(MarkAllNotificationsAsReadCommand request, CancellationToken cancellationToken)
    {
        await _notificationRepository.MarkAllAsReadAsync(request.UserId, cancellationToken);
    }
}
