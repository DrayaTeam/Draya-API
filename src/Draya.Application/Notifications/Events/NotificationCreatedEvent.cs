using Draya.Application.Notifications.DTOs;
using MediatR;

namespace Draya.Application.Notifications.Events;

public record NotificationCreatedEvent(NotificationDto Notification, Guid UserId) : INotification;
