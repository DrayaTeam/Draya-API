using Draya.Domain.Notifications;
using MediatR;

namespace Draya.Application.Notifications.Commands.ClearAllNotifications;

public record ClearAllNotificationsCommand(Guid UserId) : IRequest;

public class ClearAllNotificationsCommandHandler : IRequestHandler<ClearAllNotificationsCommand>
{
    private readonly INotificationRepository _notificationRepository;

    public ClearAllNotificationsCommandHandler(INotificationRepository notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }

    public async Task Handle(ClearAllNotificationsCommand request, CancellationToken cancellationToken)
    {
        await _notificationRepository.ClearAllAsync(request.UserId, cancellationToken);
    }
}
