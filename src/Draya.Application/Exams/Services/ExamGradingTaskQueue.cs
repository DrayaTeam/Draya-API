using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Draya.Application.Exams.Services;

public class ExamGradingTaskQueue : IExamGradingTaskQueue
{
    private readonly Channel<ExamGradingItem> _queue;

    public ExamGradingTaskQueue(int capacity = 100)
    {
        var options = new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait
        };
        _queue = Channel.CreateBounded<ExamGradingItem>(options);
    }

    public async ValueTask QueueBackgroundWorkItemAsync(ExamGradingItem item)
    {
        await _queue.Writer.WriteAsync(item);
    }

    public async ValueTask<ExamGradingItem> DequeueAsync(CancellationToken cancellationToken)
    {
        return await _queue.Reader.ReadAsync(cancellationToken);
    }
}
