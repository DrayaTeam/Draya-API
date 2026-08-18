using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace Draya.Api.Notifications;

[Authorize]
public class ExamGenerationHub : Hub
{
    // Clients connect here to receive progress updates on exam generation
}
