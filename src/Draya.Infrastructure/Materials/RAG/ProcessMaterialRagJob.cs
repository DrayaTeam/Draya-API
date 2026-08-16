using Draya.Application.Materials.RAG;
using Draya.Domain.Classrooms;
using Draya.Domain.Materials;
using Draya.Infrastructure.Materials.RAG.Extractors;
using Microsoft.Extensions.Logging;

namespace Draya.Infrastructure.Materials.RAG;

public class ProcessMaterialRagJob : IProcessMaterialRagJob
{
    private readonly ContentExtractorFactory _extractorFactory;
    private readonly ITextCleaner _textCleaner;
    private readonly IChunker _chunker;
    private readonly IEmbeddingService _embeddingService;
    private readonly IVectorStore _vectorStore;
    private readonly IClassroomRepository _classroomRepository;
    private readonly ILogger<ProcessMaterialRagJob> _logger;

    public ProcessMaterialRagJob(
        ContentExtractorFactory extractorFactory,
        ITextCleaner textCleaner,
        IChunker chunker,
        IEmbeddingService embeddingService,
        IVectorStore vectorStore,
        IClassroomRepository classroomRepository,
        ILogger<ProcessMaterialRagJob> logger)
    {
        _extractorFactory = extractorFactory;
        _textCleaner = textCleaner;
        _chunker = chunker;
        _embeddingService = embeddingService;
        _vectorStore = vectorStore;
        _classroomRepository = classroomRepository;
        _logger = logger;
    }

    public async Task ProcessAsync(LearningMaterial material, MaterialVersion version, string filePath, CancellationToken ct)
    {
        _logger.LogInformation("Starting RAG pipeline for Material {MaterialId}, Version {VersionId}", material.Id, version.Id);

        // 1. Extract
        var extractor = _extractorFactory.GetExtractor(material.MaterialType);
        ExtractedDocument document;
        using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
        {
            document = await extractor.ExtractAsync(fileStream, ct);
        }

        // 2. Clean
        document.FullText = _textCleaner.Clean(document.FullText);
        foreach (var segment in document.Segments)
        {
            segment.Text = _textCleaner.Clean(segment.Text);
        }

        // 3. Chunk
        var chunks = _chunker.Chunk(document);
        if (!chunks.Any())
        {
            throw new InvalidOperationException("Chunking failed: No chunks were generated from the document.");
        }

        // 4. Gather Metadata
        var classroom = await _classroomRepository.GetByIdAsync(material.ClassroomId, ct);
        if (classroom == null)
        {
            throw new InvalidOperationException($"Classroom {material.ClassroomId} not found.");
        }

        var chunkHashes = chunks.Select(c => c.ChunkHash).Distinct().ToList();

        // 5. Reconcile (Fetch existing embeddings)
        var existingEmbeddings = await _vectorStore.GetEmbeddingsByHashesAsync(material.Id, chunkHashes, ct);

        // 6. Identify Missing Embeddings
        var missingChunks = chunks.Where(c => !existingEmbeddings.ContainsKey(c.ChunkHash)).ToList();
        var missingHashes = missingChunks.Select(c => c.ChunkHash).Distinct().ToList();
        
        _logger.LogInformation("Found {ExistingCount} existing embeddings. Needs {MissingCount} new embeddings.", existingEmbeddings.Count, missingHashes.Count);

        // 7. Embed Missing
        if (missingHashes.Any())
        {
            // We only need to embed unique texts corresponding to the missing hashes
            var textsToEmbed = missingHashes
                .Select(h => missingChunks.First(c => c.ChunkHash == h).Text)
                .ToList();

            var newEmbeddings = await _embeddingService.EmbedBatchAsync(textsToEmbed, ct);

            // Add them to our dictionary
            for (int i = 0; i < missingHashes.Count; i++)
            {
                existingEmbeddings[missingHashes[i]] = newEmbeddings[i];
            }
        }

        // 8. Prepare Payloads and Final Embeddings List
        var payloads = new List<QdrantChunkPayload>();
        var finalEmbeddings = new List<float[]>();

        foreach (var chunk in chunks)
        {
            var payload = new QdrantChunkPayload
            {
                ClassroomId = classroom.Id,
                SubjectId = classroom.SubjectId,
                MaterialId = material.Id,
                MaterialVersionId = version.Id,
                FileName = material.Title, // We don't have OriginalFileName in LearningMaterial easily accessible here, fallback to Title
                Title = material.Title,
                ChunkIndex = chunk.ChunkIndex,
                ChunkHash = chunk.ChunkHash,
                Text = chunk.Text,
                CharStart = chunk.CharStart,
                CharEnd = chunk.CharEnd,
                PageStart = chunk.PageStart,
                PageEnd = chunk.PageEnd
            };

            payloads.Add(payload);
            finalEmbeddings.Add(existingEmbeddings[chunk.ChunkHash]);
        }

        // 9. Upsert to Qdrant
        await _vectorStore.UpsertChunksAsync(version.Id, payloads, finalEmbeddings, ct);

        _logger.LogInformation("Successfully completed RAG pipeline for Material {MaterialId}, Version {VersionId}", material.Id, version.Id);
    }
}
