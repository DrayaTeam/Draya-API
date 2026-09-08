namespace Draya.Application.Materials;

public record MaterialProcessingItem(Guid MaterialId, Guid VersionId, string FilePath, string FileName, string ContentType);

public interface IBackgroundTaskQueue
{
    ValueTask QueueBackgroundWorkItemAsync(MaterialProcessingItem item);
    ValueTask<MaterialProcessingItem> DequeueAsync(CancellationToken cancellationToken);
}
