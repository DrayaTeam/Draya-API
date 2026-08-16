using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Draya.Application.Materials;
using Draya.Application.Materials.DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Draya.Infrastructure.Materials;

public class CloudinaryMediaStorageService : IMediaStorageService
{
    private readonly Cloudinary _cloudinary;
    private readonly ILogger<CloudinaryMediaStorageService> _logger;

    public CloudinaryMediaStorageService(IConfiguration configuration, ILogger<CloudinaryMediaStorageService> logger)
    {
        _logger = logger;
        
        var cloudinarySettings = configuration.GetSection("CloudinarySettings");
        var cloudName = cloudinarySettings["CloudName"];
        var apiKey = cloudinarySettings["ApiKey"];
        var apiSecret = cloudinarySettings["ApiSecret"];

        if (string.IsNullOrEmpty(cloudName) || string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiSecret))
        {
            _logger.LogWarning("Cloudinary credentials are not properly configured.");
        }

        var account = new Account(cloudName, apiKey, apiSecret);
        _cloudinary = new Cloudinary(account);
        _cloudinary.Api.Secure = true;
    }

    public async Task<UploadedMediaMetadata> UploadAsync(Stream stream, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        var isVideo = contentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase);
        var isImage = contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase) 
                   || contentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase);

        var resourceType = CloudinaryDotNet.Actions.ResourceType.Raw;
        if (isVideo)
        {
            resourceType = CloudinaryDotNet.Actions.ResourceType.Video;
        }
        else if (isImage)
        {
            resourceType = CloudinaryDotNet.Actions.ResourceType.Image;
        }

        var lastSlash = fileName.LastIndexOf('/');
        string? folder = lastSlash >= 0 ? fileName.Substring(0, lastSlash) : null;
        string publicId = lastSlash >= 0 ? fileName.Substring(lastSlash + 1) : fileName;

        if (resourceType == CloudinaryDotNet.Actions.ResourceType.Video)
        {
            var videoUploadParams = new VideoUploadParams
            {
                File = new FileDescription(publicId, stream),
                Folder = folder,
                AssetFolder = folder,
                PublicId = publicId,
                UseFilename = true,
                UniqueFilename = false
            };
            var result = await _cloudinary.UploadAsync(videoUploadParams, cancellationToken);
            return MapToMetadata(result, "video");
        }
        else if (resourceType == CloudinaryDotNet.Actions.ResourceType.Image)
        {
            var imageUploadParams = new ImageUploadParams
            {
                File = new FileDescription(publicId, stream),
                Folder = folder,
                AssetFolder = folder,
                PublicId = publicId,
                UseFilename = true,
                UniqueFilename = false
            };
            var result = await _cloudinary.UploadAsync(imageUploadParams, cancellationToken);
            return MapToMetadata(result, "image");
        }
        else
        {
            var rawUploadParams = new RawUploadParams
            {
                File = new FileDescription(publicId, stream),
                Folder = folder,
                AssetFolder = folder,
                PublicId = publicId,
                UseFilename = true,
                UniqueFilename = false
            };
            var result = await Task.Run(() => _cloudinary.Upload(rawUploadParams), cancellationToken);
            return MapToMetadata(result, "raw");
        }
    }

    public Task<string> GetSecureDeliveryUrlAsync(UploadedMediaMetadata metadata)
    {
        var resourceType = metadata.ResourceType ?? "raw";
        var url = _cloudinary.Api.Url
            .Secure(true)
            .ResourceType(metadata.ResourceType)
            .Action("upload")
            .BuildUrl(metadata.ProviderAssetId);

        // Append the correct format to the URL if applicable
        if (!string.IsNullOrEmpty(metadata.Format))
        {
            if (!string.Equals(resourceType, "raw", StringComparison.OrdinalIgnoreCase))
            {
                url = $"{url}.{metadata.Format}";
            }
        }

        return Task.FromResult(url);
    }

    private static UploadedMediaMetadata MapToMetadata(UploadResult result, string resourceType)
    {
        return new UploadedMediaMetadata
        {
            Provider = "Cloudinary",
            ProviderAssetId = result.PublicId,
            ResourceType = resourceType,
            Format = result.Format
        };
    }
}
