using Draya.Domain.Materials;

namespace Draya.Application.Materials.RAG;

public interface IContentExtractor
{
    MaterialType SupportedType { get; }
    Task<ExtractedDocument> ExtractAsync(Stream fileStream, CancellationToken ct);
}

public interface ITextCleaner
{
    string Clean(string rawText);
}

public interface IChunker
{
    List<TextChunk> Chunk(ExtractedDocument document);
}

public interface IEmbeddingService
{
    Task<float[]> EmbedAsync(string text, CancellationToken ct);
    Task<List<float[]>> EmbedBatchAsync(List<string> texts, CancellationToken ct);
}

public interface IVectorStore
{
    // Fetches existing vectors for a given MaterialId to avoid re-embedding unchanged chunks
    Task<Dictionary<string, float[]>> GetEmbeddingsByHashesAsync(Guid materialId, IEnumerable<string> chunkHashes, CancellationToken ct);
    
    // Upserts points for a specific version.
    Task UpsertChunksAsync(Guid materialVersionId, List<QdrantChunkPayload> payloads, List<float[]> embeddings, CancellationToken ct);
}
