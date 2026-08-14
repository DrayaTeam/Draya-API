namespace Draya.Domain.Materials;

public class MaterialVersion
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MaterialId { get; set; }
    public int VersionNumber { get; set; }
    public string? FileUrl { get; set; }
    public ParseStatus ParseStatus { get; set; } = ParseStatus.Pending;
    public string? ParseErrorMessage { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public LearningMaterial Material { get; set; } = null!;
    public ICollection<MaterialChunk> Chunks { get; set; } = new List<MaterialChunk>();
}
