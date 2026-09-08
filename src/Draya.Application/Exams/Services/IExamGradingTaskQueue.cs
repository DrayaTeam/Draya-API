using System;
using System.Threading;
using System.Threading.Tasks;

namespace Draya.Application.Exams.Services;

public record ExamGradingItem(Guid GradingJobId, Guid StudentExamAttemptId);

public interface IExamGradingTaskQueue
{
    ValueTask QueueBackgroundWorkItemAsync(ExamGradingItem item);
    ValueTask<ExamGradingItem> DequeueAsync(CancellationToken cancellationToken);
}
