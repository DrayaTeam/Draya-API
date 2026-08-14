using MediatR;

namespace Draya.Application.Materials.Notifications;

public record MaterialParsedNotification(Guid MaterialId, Guid VersionId, string Status, string? Message = null) : INotification;
