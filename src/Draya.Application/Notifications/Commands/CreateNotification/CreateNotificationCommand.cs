using Draya.Application.Notifications.DTOs;
using Draya.Domain.Notifications;
using MediatR;

namespace Draya.Application.Notifications.Commands.CreateNotification;

public record CreateNotificationCommand(
    Guid UserId,
    string Title,
    string Message,
    NotificationType Type,
    string? Link
) : IRequest<NotificationDto>;

public class CreateNotificationCommandHandler : IRequestHandler<CreateNotificationCommand, NotificationDto>
{
    private readonly INotificationRepository _notificationRepository;
    private readonly IMediator _mediator;

    public CreateNotificationCommandHandler(INotificationRepository notificationRepository, IMediator mediator)
    {
        _notificationRepository = notificationRepository;
        _mediator = mediator;
    }

    public async Task<NotificationDto> Handle(CreateNotificationCommand request, CancellationToken cancellationToken)
    {
        var notification = new Notification
        {
            UserId = request.UserId,
            Title = request.Title,
            Message = request.Message,
            Type = request.Type,
            Link = request.Link
        };

        await _notificationRepository.AddAsync(notification, cancellationToken);

        var dto = new NotificationDto(
            notification.Id,
            notification.Title,
            notification.Message,
            notification.Type.ToString().ToLowerInvariant(),
            notification.Link,
            notification.Read,
            notification.CreatedAt
        );

        // Publish event to be handled by API layer to broadcast via SignalR
        await _mediator.Publish(new Events.NotificationCreatedEvent(dto, request.UserId), cancellationToken);

        return dto;
    }
}
