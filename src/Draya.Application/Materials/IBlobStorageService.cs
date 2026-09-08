namespace Draya.Application.Materials;

public interface IBlobStorageService
{
    Task<string> UploadFileAsync(string containerName, string fileName, Stream content, string contentType);
    string GetServiceSasUriForBlob(string containerName, string blobName, DateTimeOffset expiresOn);
}
