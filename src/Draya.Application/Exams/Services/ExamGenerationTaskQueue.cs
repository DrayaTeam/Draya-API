using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Draya.Application.Exams.Services;

public class ExamGenerationTaskQueue : IExamGenerationTaskQueue
{
    private readonly Channel<ExamGenerationItem> _queue;

    public ExamGenerationTaskQueue(int capacity = 100)
    {
        var options = new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait
        };
        _queue = Channel.CreateBounded<ExamGenerationItem>(options);
    }

    public async ValueTask QueueBackgroundWorkItemAsync(ExamGenerationItem item)
    {
        await _queue.Writer.WriteAsync(item);
    }

    public async ValueTask<ExamGenerationItem> DequeueAsync(CancellationToken cancellationToken)
    {
        return await _queue.Reader.ReadAsync(cancellationToken);
    }
}
