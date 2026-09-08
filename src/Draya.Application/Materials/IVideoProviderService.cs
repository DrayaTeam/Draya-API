namespace Draya.Application.Materials;

public interface IVideoProviderService
{
    Task<string> UploadVideoAsync(string filePath, string title, string description, CancellationToken cancellationToken = default);
}
