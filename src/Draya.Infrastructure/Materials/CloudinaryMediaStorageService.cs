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
        var cloudName  = cloudinarySettings["CloudName"];
        var apiKey     = cloudinarySettings["ApiKey"];
        var apiSecret  = cloudinarySettings["ApiSecret"];

        if (string.IsNullOrEmpty(cloudName) || string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiSecret))
            _logger.LogWarning("Cloudinary credentials are not properly configured.");

        var account = new Account(cloudName, apiKey, apiSecret);
        _cloudinary = new Cloudinary(account);
        _cloudinary.Api.Secure = true;
    }

    public async Task<UploadedMediaMetadata> UploadAsync(Stream stream, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        var isVideo = contentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase);
        var isImage = contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);

        var resourceType = ResourceType.Raw;
        if (isVideo)      resourceType = ResourceType.Video;
        else if (isImage) resourceType = ResourceType.Image;

        // Split "teachers/Omar_Moh/Math/soundboard.mp4"
        //   => Folder   = "teachers/Omar_Moh/Math"   (organises in Cloudinary Media Library)
        //   => publicId = "soundboard"               (for video/image — Cloudinary stores format separately)
        //   => publicId = "lec1.pdf"                 (for raw — extension is kept)
        var lastSlash = fileName.LastIndexOf('/');
        var folder   = lastSlash >= 0 ? fileName[..lastSlash] : null;
        var baseName = lastSlash >= 0 ? fileName[(lastSlash + 1)..] : fileName;

        string publicId;
        if (resourceType == ResourceType.Raw)
        {
            publicId = baseName;                              // e.g. "lec1.pdf"
        }
        else
        {
            var dot = baseName.LastIndexOf('.');
            publicId = dot > 0 ? baseName[..dot] : baseName; // e.g. "soundboard"
        }

        _logger.LogInformation(
            "Uploading to Cloudinary | Folder={Folder} | PublicId={PublicId} | ResourceType={ResourceType}",
            folder, publicId, resourceType);

        UploadResult result;

        if (resourceType == ResourceType.Video)
        {
            result = await _cloudinary.UploadAsync(new VideoUploadParams
            {
                File           = new FileDescription("upload", stream),
                Folder         = folder,
                PublicId       = publicId,
                UniqueFilename = false,
                Overwrite      = true,
            }, cancellationToken);
        }
        else if (resourceType == ResourceType.Image)
        {
            result = await _cloudinary.UploadAsync(new ImageUploadParams
            {
                File           = new FileDescription("upload", stream),
                Folder         = folder,
                PublicId       = publicId,
                UniqueFilename = false,
                Overwrite      = true,
            }, cancellationToken);
        }
        else
        {
            result = await Task.Run(() => _cloudinary.Upload(new RawUploadParams
            {
                File           = new FileDescription("upload", stream),
                Folder         = folder,
                PublicId       = publicId,
                UniqueFilename = false,
                Overwrite      = true,
            }), cancellationToken);
        }

        if (result.Error != null)
            _logger.LogError("Cloudinary upload error: {Error}", result.Error.Message);
        else
            _logger.LogInformation(
                "Cloudinary upload succeeded | SecureUrl={Url} | PublicId={PublicId}",
                result.SecureUrl, result.PublicId);

        return MapToMetadata(result, resourceType == ResourceType.Video ? "video"
                                   : resourceType == ResourceType.Image ? "image"
                                   : "raw");
    }

    public Task<string> GetSecureDeliveryUrlAsync(UploadedMediaMetadata metadata)
        => Task.FromResult(BuildDeliveryUrl(metadata));

    public string BuildDeliveryUrl(UploadedMediaMetadata metadata)
    {
        // Always prefer the SecureUrl captured at upload time — it's the actual URL
        // Cloudinary assigned and is correct in both Legacy and DAM folder modes.
        if (!string.IsNullOrEmpty(metadata.SecureUrl))
            return metadata.SecureUrl;

        // Fallback: reconstruct from PublicId (legacy-mode accounts only)
        var resourceType = metadata.ResourceType ?? "raw";
        var url = _cloudinary.Api.Url
            .Secure(true)
            .ResourceType(resourceType)
            .Action("upload")
            .BuildUrl(metadata.ProviderAssetId);

        if (!string.IsNullOrEmpty(metadata.Format) &&
            !string.Equals(resourceType, "raw", StringComparison.OrdinalIgnoreCase))
        {
            var extension = $".{metadata.Format}";
            if (!url.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
                url = $"{url}{extension}";
        }

        return url;
    }

    private static UploadedMediaMetadata MapToMetadata(UploadResult result, string resourceType)
    {
        return new UploadedMediaMetadata
        {
            Provider        = "Cloudinary",
            ProviderAssetId = result.PublicId,
            ResourceType    = resourceType,
            Format          = result.Format,
            SecureUrl       = result.SecureUrl?.ToString(), // the real URL, mode-agnostic
        };
    }
}
