using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Draya.Api.Notifications;

[Authorize]
public class ReportsNotificationHub : Hub
{
}
