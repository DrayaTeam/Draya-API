namespace Draya.Application.Notifications.DTOs;

public record NotificationDto(
    Guid Id,
    string Title,
    string Message,
    string Type,
    string? Link,
    bool Read,
    DateTimeOffset CreatedAt
);

public record GetNotificationsResponse(
    IEnumerable<NotificationDto> Items,
    int UnreadCount,
    int TotalCount
);
