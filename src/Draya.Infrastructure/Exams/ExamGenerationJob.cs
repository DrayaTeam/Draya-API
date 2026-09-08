using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Draya.Application.Exams.Services;

namespace Draya.Infrastructure.Exams;

public class ExamGenerationJob : BackgroundService
{
    private readonly IExamGenerationTaskQueue _taskQueue;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ExamGenerationJob> _logger;

    public ExamGenerationJob(
        IExamGenerationTaskQueue taskQueue,
        IServiceProvider serviceProvider,
        ILogger<ExamGenerationJob> logger)
    {
        _taskQueue = taskQueue;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ExamGenerationJob is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var workItem = await _taskQueue.DequeueAsync(stoppingToken);

                using var scope = _serviceProvider.CreateScope();
                var examGenerationService = scope.ServiceProvider.GetRequiredService<IExamGenerationService>();

                _logger.LogInformation("Processing ExamGeneration {GenerationId}.", workItem.GenerationId);

                await examGenerationService.ProcessGenerationAsync(workItem.GenerationId, workItem.Request, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Prevent throwing if stoppingToken was signaled
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred executing exam generation work item.");
            }
        }
        
        _logger.LogInformation("ExamGenerationJob is stopping.");
    }
}
