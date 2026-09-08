using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Draya.Api.Notifications;

[Authorize]
public class MaterialNotificationHub : Hub
{
    // Clients will connect to this hub and listen for 'MaterialParsed' events.
    // The server will push updates using IHubContext<MaterialNotificationHub>.
    
    public override Task OnConnectedAsync()
    {
        return base.OnConnectedAsync();
    }
}
