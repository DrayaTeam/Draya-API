namespace Draya.Domain.Materials;

public class VideoDetail
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MaterialId { get; set; }
    public int DurationSeconds { get; set; }
    public string? Resolution { get; set; }
    public string Provider { get; set; } = "YouTube";
    public string? ProviderVideoId { get; set; }
    public string? EmbedUrl { get; set; }

    // Navigation properties
    public LearningMaterial Material { get; set; } = null!;
}
