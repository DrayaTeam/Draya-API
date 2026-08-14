using System.Threading.Channels;

namespace Draya.Application.Materials;

public class DefaultBackgroundTaskQueue : IBackgroundTaskQueue
{
    private readonly Channel<MaterialProcessingItem> _queue;

    public DefaultBackgroundTaskQueue(int capacity = 100)
    {
        var options = new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait
        };
        _queue = Channel.CreateBounded<MaterialProcessingItem>(options);
    }

    public async ValueTask QueueBackgroundWorkItemAsync(MaterialProcessingItem item)
    {
        await _queue.Writer.WriteAsync(item);
    }

    public async ValueTask<MaterialProcessingItem> DequeueAsync(CancellationToken cancellationToken)
    {
        return await _queue.Reader.ReadAsync(cancellationToken);
    }
}
