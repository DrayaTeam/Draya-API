using System.Threading;
using System.Threading.Tasks;
using Draya.Application.Exams.Events;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Draya.Api.Notifications;

public class ExamGenerationProgressEventHandler : INotificationHandler<ExamGenerationProgressEvent>
{
    private readonly IHubContext<ExamGenerationHub> _hubContext;
    private readonly ILogger<ExamGenerationProgressEventHandler> _logger;

    public ExamGenerationProgressEventHandler(IHubContext<ExamGenerationHub> hubContext, ILogger<ExamGenerationProgressEventHandler> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task Handle(ExamGenerationProgressEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Broadcasting ExamGenerationProgress: GenerationId={GenerationId}, Status={Status}", 
            notification.GenerationId, notification.Status);

        await _hubContext.Clients.User(notification.TeacherId.ToString()).SendAsync(
            "ReceiveGenerationProgress",
            new
            {
                GenerationId = notification.GenerationId,
                Status = notification.Status.ToString(),
                ErrorMessage = notification.ErrorMessage,
                ExamId = notification.ExamId
            },
            cancellationToken);
    }
}
