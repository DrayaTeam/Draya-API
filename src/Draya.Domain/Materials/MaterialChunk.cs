namespace Draya.Domain.Materials;

public class MaterialChunk
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid VersionId { get; set; }
    public string TextContent { get; set; } = string.Empty;
    public string? EmbeddingId { get; set; } // Reference to Qdrant point ID
    public int ChunkIndex { get; set; }

    // Navigation properties
    public MaterialVersion Version { get; set; } = null!;
}
