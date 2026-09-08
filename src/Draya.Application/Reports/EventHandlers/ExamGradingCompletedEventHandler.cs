using System.Threading;
using System.Threading.Tasks;
using Draya.Application.Exams.Events;
using Draya.Application.Reports.Services;
using Draya.Domain.Exams;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Draya.Application.Reports.EventHandlers;

public class ExamGradingCompletedEventHandler : INotificationHandler<ExamGradingProgressEvent>
{
    private readonly IReportGenerationService _reportGenerationService;
    private readonly ILogger<ExamGradingCompletedEventHandler> _logger;

    public ExamGradingCompletedEventHandler(IReportGenerationService reportGenerationService, ILogger<ExamGradingCompletedEventHandler> logger)
    {
        _reportGenerationService = reportGenerationService;
        _logger = logger;
    }

    public async Task Handle(ExamGradingProgressEvent notification, CancellationToken cancellationToken)
    {
        // Only trigger report generation if grading was successful (or with warnings)
        if (notification.Status == GradingStatus.Completed || notification.Status == GradingStatus.CompletedWithWarning)
        {
            _logger.LogInformation("Triggering report generation for student {StudentId} and attempt {AttemptId}", notification.StudentId, notification.StudentExamAttemptId);
            await _reportGenerationService.GenerateReportAsync(notification.StudentId, notification.StudentExamAttemptId, cancellationToken);
        }
    }
}
