using System.Threading;
using System.Threading.Tasks;
using Draya.Application.Exams.Events;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Draya.Api.Notifications;

public class ExamGradingProgressEventHandler : INotificationHandler<ExamGradingProgressEvent>
{
    private readonly IHubContext<ExamGradingHub> _hubContext;
    private readonly ILogger<ExamGradingProgressEventHandler> _logger;

    public ExamGradingProgressEventHandler(
        IHubContext<ExamGradingHub> hubContext,
        ILogger<ExamGradingProgressEventHandler> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task Handle(ExamGradingProgressEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Broadcasting grading progress for Job {JobId}: {Status}", notification.GradingJobId, notification.Status);

        await _hubContext.Clients.Group($"GradingJob_{notification.GradingJobId}").SendAsync("GradingProgressUpdated", new
        {
            notification.GradingJobId,
            Status = notification.Status.ToString(),
            notification.ErrorMessage,
            notification.FinalScore,
            notification.NeedsTeacherReview
        }, cancellationToken);
    }
}
