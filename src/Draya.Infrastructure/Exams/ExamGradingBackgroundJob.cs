using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Draya.Application.Exams.Services;

namespace Draya.Infrastructure.Exams;

public class ExamGradingBackgroundJob : BackgroundService
{
    private readonly IExamGradingTaskQueue _taskQueue;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ExamGradingBackgroundJob> _logger;

    public ExamGradingBackgroundJob(
        IExamGradingTaskQueue taskQueue,
        IServiceProvider serviceProvider,
        ILogger<ExamGradingBackgroundJob> logger)
    {
        _taskQueue = taskQueue;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ExamGradingBackgroundJob is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var workItem = await _taskQueue.DequeueAsync(stoppingToken);

                using var scope = _serviceProvider.CreateScope();
                var examGradingService = scope.ServiceProvider.GetRequiredService<IExamGradingService>();

                _logger.LogInformation("Processing ExamGradingJob {GradingJobId}.", workItem.GradingJobId);

                await examGradingService.ProcessGradingAsync(workItem.GradingJobId, workItem.StudentExamAttemptId, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Prevent throwing if stoppingToken was signaled
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred executing exam grading work item.");
            }
        }
        
        _logger.LogInformation("ExamGradingBackgroundJob is stopping.");
    }
}
