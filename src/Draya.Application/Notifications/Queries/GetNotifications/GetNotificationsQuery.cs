using Draya.Application.Notifications.DTOs;
using Draya.Domain.Notifications;
using MediatR;

namespace Draya.Application.Notifications.Queries.GetNotifications;

public record GetNotificationsQuery(Guid UserId, int Page = 1, int PageSize = 20, bool UnreadOnly = false) : IRequest<GetNotificationsResponse>;

public class GetNotificationsQueryHandler : IRequestHandler<GetNotificationsQuery, GetNotificationsResponse>
{
    private readonly INotificationRepository _notificationRepository;

    public GetNotificationsQueryHandler(INotificationRepository notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }

    public async Task<GetNotificationsResponse> Handle(GetNotificationsQuery request, CancellationToken cancellationToken)
    {
        var notifications = await _notificationRepository.GetByUserIdAsync(request.UserId, request.Page, request.PageSize, request.UnreadOnly, cancellationToken);
        var totalCount = await _notificationRepository.GetTotalCountAsync(request.UserId, request.UnreadOnly, cancellationToken);
        var unreadCount = await _notificationRepository.GetUnreadCountAsync(request.UserId, cancellationToken);

        var items = notifications.Select(n => new NotificationDto(
            n.Id,
            n.Title,
            n.Message,
            n.Type.ToString().ToLowerInvariant(),
            n.Link,
            n.Read,
            n.CreatedAt
        ));

        return new GetNotificationsResponse(items, unreadCount, totalCount);
    }
}
