using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace Draya.Api.Notifications;

public class ExamGradingHub : Hub
{
    // The student or teacher can connect to this hub and join a group specific to the attempt or job
    public async Task SubscribeToJob(string gradingJobId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"GradingJob_{gradingJobId}");
    }

    public async Task UnsubscribeFromJob(string gradingJobId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"GradingJob_{gradingJobId}");
    }
}
