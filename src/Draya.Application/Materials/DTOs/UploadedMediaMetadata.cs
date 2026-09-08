namespace Draya.Application.Materials.DTOs;

public class UploadedMediaMetadata
{
    public string Provider { get; set; } = string.Empty;
    public string ProviderAssetId { get; set; } = string.Empty;
    public string ResourceType { get; set; } = string.Empty;
    public string Format { get; set; } = string.Empty;
    /// <summary>The actual HTTPS URL returned by the storage provider after upload.</summary>
    public string? SecureUrl { get; set; }
}
