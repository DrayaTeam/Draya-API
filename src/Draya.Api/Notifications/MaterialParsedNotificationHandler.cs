using Draya.Application.Materials.Notifications;
using MediatR;
using Microsoft.AspNetCore.SignalR;

namespace Draya.Api.Notifications;

public class MaterialParsedNotificationHandler : INotificationHandler<MaterialParsedNotification>
{
    private readonly IHubContext<MaterialNotificationHub> _hubContext;

    public MaterialParsedNotificationHandler(IHubContext<MaterialNotificationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task Handle(MaterialParsedNotification notification, CancellationToken cancellationToken)
    {
        await _hubContext.Clients.All.SendAsync("MaterialParsed", new 
        { 
            materialId = notification.MaterialId, 
            versionId = notification.VersionId, 
            status = notification.Status, 
            message = notification.Message 
        }, cancellationToken);
    }
}
