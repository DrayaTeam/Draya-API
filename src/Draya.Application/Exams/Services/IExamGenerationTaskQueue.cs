using System;
using System.Threading;
using System.Threading.Tasks;

namespace Draya.Application.Exams.Services;

public record ExamGenerationItem(Guid GenerationId, GenerateExamRequest Request);

public interface IExamGenerationTaskQueue
{
    ValueTask QueueBackgroundWorkItemAsync(ExamGenerationItem item);
    ValueTask<ExamGenerationItem> DequeueAsync(CancellationToken cancellationToken);
}
