namespace Draya.Application.Materials.RAG;

public class ExtractedDocument
{
    public string FullText { get; set; } = string.Empty;
    public List<TextSegment> Segments { get; set; } = new();
}

public class TextSegment
{
    public int? PageOrSlide { get; set; }
    public string Text { get; set; } = string.Empty;
    public int CharStart { get; set; }
    public int CharEnd { get; set; }
}

public class TextChunk
{
    public int ChunkIndex { get; set; }
    public string Text { get; set; } = string.Empty;
    public string ChunkHash { get; set; } = string.Empty;
    public int CharStart { get; set; }
    public int CharEnd { get; set; }
    public int? PageStart { get; set; }
    public int? PageEnd { get; set; }
    public int CharCount => CharEnd - CharStart;
}

public class QdrantChunkPayload
{
    public Guid ClassroomId { get; set; }
    public Guid SubjectId { get; set; }
    public Guid MaterialId { get; set; }
    public Guid MaterialVersionId { get; set; }
    
    public string FileName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    
    public int ChunkIndex { get; set; }
    public string ChunkHash { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public int CharStart { get; set; }
    public int CharEnd { get; set; }
    public int? PageStart { get; set; }
    public int? PageEnd { get; set; }
}

public class RetrievalQuery
{
    public List<Guid> MaterialVersionIds { get; set; } = new();
    public string QueryText { get; set; } = string.Empty;
    public int TopK { get; set; } = 10;
    public float MinScore { get; set; } = 0.5f;
}

public class RetrievedChunk
{
    public string ChunkId { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public float Score { get; set; }
}

