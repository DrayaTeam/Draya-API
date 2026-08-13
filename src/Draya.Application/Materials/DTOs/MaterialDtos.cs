namespace Draya.Application.Materials.DTOs;

public class MaterialVersionDto
{
    public Guid VersionId { get; set; }
    public int VersionNumber { get; set; }
    public string FileUrl { get; set; } = string.Empty;
    public string ParseStatus { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
    public string? ErrorMessage { get; set; }
}

public class MaterialDto
{
    public Guid MaterialId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string MaterialType { get; set; } = string.Empty;
    public MaterialVersionDto CurrentVersion { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}

public class VideoStreamDto
{
    public string StreamUrl { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
}
