using Draya.Application.Materials.DTOs;

namespace Draya.Application.Materials;

public interface IMediaStorageService
{
    Task<UploadedMediaMetadata> UploadAsync(Stream stream, string fileName, string contentType, CancellationToken cancellationToken = default);
    Task<string> GetSecureDeliveryUrlAsync(UploadedMediaMetadata metadata);
}
