using System.Threading;
using System.Threading.Tasks;
using Draya.Application.Exams.Events;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Draya.Api.Notifications;

public class AnswerScoreOverriddenEventHandler : INotificationHandler<AnswerScoreOverriddenEvent>
{
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<AnswerScoreOverriddenEventHandler> _logger;

    public AnswerScoreOverriddenEventHandler(
        IHubContext<NotificationHub> hubContext,
        ILogger<AnswerScoreOverriddenEventHandler> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task Handle(AnswerScoreOverriddenEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Broadcasting teacher override for Answer {AnswerId} to Student {StudentId}", notification.AnswerId, notification.StudentId);

        await _hubContext.Clients.Group($"User_{notification.StudentId}").SendAsync("AnswerScoreOverridden", new
        {
            notification.AttemptId,
            notification.AnswerId,
            notification.NewScore
        }, cancellationToken);
    }
}
