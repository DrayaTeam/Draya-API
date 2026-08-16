using System.Security.Cryptography;
using System.Text;
using Draya.Application.Materials.RAG;
using Microsoft.Extensions.Logging;
using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace Draya.Infrastructure.Materials.RAG;

public class QdrantVectorStore : IVectorStore
{
    private readonly QdrantClient _qdrantClient;
    private readonly ILogger<QdrantVectorStore> _logger;
    private const string CollectionName = "materials";
    private const int BgeM3Dimensions = 1024;

    public QdrantVectorStore(QdrantClient qdrantClient, ILogger<QdrantVectorStore> logger)
    {
        _qdrantClient = qdrantClient;
        _logger = logger;
    }

    public async Task<Dictionary<string, float[]>> GetEmbeddingsByHashesAsync(Guid materialId, IEnumerable<string> chunkHashes, CancellationToken ct)
    {
        var hashesList = chunkHashes.Distinct().ToList();
        var result = new Dictionary<string, float[]>();

        if (!hashesList.Any())
        {
            return result;
        }

        try
        {
            // Ensure the collection exists before querying — first run will create it
            await EnsureCollectionExistsAsync(ct);

            // We use Scroll or Search to find points where materialId matches AND chunkHash is in the list
            // Since we just need exactly matching hashes, we can build a filter
            
            var conditions = new List<Condition>
            {
                MatchCondition("materialId", materialId.ToString())
            };

            var hashConditions = hashesList.Select(h => MatchCondition("chunkHash", h)).ToList();
            
            // materialId == ... AND (chunkHash == h1 OR chunkHash == h2 ...)
            var filter = new Filter
            {
                Must = { conditions },
                Should = { hashConditions }
            };

            // Using Scroll to get all matching points (assuming less than 10,000 chunks per document)
            var response = await _qdrantClient.ScrollAsync(
                collectionName: CollectionName,
                filter: filter,
                limit: (uint)hashesList.Count * 2,
                cancellationToken: ct
            );

            foreach (var point in response.Result)
            {
                if (point.Payload.TryGetValue("chunkHash", out var hashVal) && 
                    point.Vectors != null && 
                    point.Vectors.VectorsOptionsCase == VectorsOutput.VectorsOptionsOneofCase.Vector)
                {
                    string hash = hashVal.StringValue;
                    if (!result.ContainsKey(hash))
                    {
                        var vectorData = point.Vectors.Vector.Data;
                        // Convert repeated field of float to float array
                        result[hash] = vectorData.ToArray();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to query existing embeddings from Qdrant for Material {MaterialId}", materialId);
            // We don't throw here; if lookup fails, we just re-embed to be safe rather than failing the whole pipeline
        }

        return result;
    }

    public async Task UpsertChunksAsync(Guid materialVersionId, List<QdrantChunkPayload> payloads, List<float[]> embeddings, CancellationToken ct)
    {
        if (payloads.Count != embeddings.Count)
        {
            throw new ArgumentException("Payloads count must match embeddings count.");
        }

        if (!payloads.Any()) return;

        // Ensure collection exists and has 1024 dimensions
        await EnsureCollectionExistsAsync(ct);

        var points = new List<PointStruct>();

        for (int i = 0; i < payloads.Count; i++)
        {
            var payload = payloads[i];
            var embedding = embeddings[i];

            if (embedding.Length != BgeM3Dimensions)
            {
                throw new InvalidOperationException($"Expected embedding dimension {BgeM3Dimensions}, but got {embedding.Length}");
            }

            var pointId = GenerateDeterministicPointId(materialVersionId, payload.ChunkIndex);

            var qdrantPayload = new Dictionary<string, Value>
            {
                ["classroomId"] = payload.ClassroomId.ToString(),
                ["subjectId"] = payload.SubjectId.ToString(),
                ["materialId"] = payload.MaterialId.ToString(),
                ["materialVersionId"] = payload.MaterialVersionId.ToString(),
                ["fileName"] = payload.FileName,
                ["title"] = payload.Title,
                ["chunkIndex"] = payload.ChunkIndex,
                ["chunkHash"] = payload.ChunkHash,
                ["text"] = payload.Text,
                ["charStart"] = payload.CharStart,
                ["charEnd"] = payload.CharEnd
            };

            if (payload.PageStart.HasValue)
            {
                qdrantPayload["pageStart"] = payload.PageStart.Value;
            }
            if (payload.PageEnd.HasValue)
            {
                qdrantPayload["pageEnd"] = payload.PageEnd.Value;
            }

            var point = new PointStruct
            {
                Id = new PointId { Uuid = pointId.ToString() },
                Vectors = embedding,
                Payload = { qdrantPayload }
            };

            points.Add(point);
        }

        // Upsert in batches to avoid overwhelming Qdrant (e.g. 100 at a time)
        const int batchSize = 100;
        for (int i = 0; i < points.Count; i += batchSize)
        {
            var batch = points.Skip(i).Take(batchSize).ToList();
            await _qdrantClient.UpsertAsync(CollectionName, batch, cancellationToken: ct);
            _logger.LogInformation("Upserted batch of {Count} chunks for MaterialVersion {VersionId} into Qdrant", batch.Count, materialVersionId);
        }
    }

    private async Task EnsureCollectionExistsAsync(CancellationToken ct)
    {
        try
        {
            var collections = await _qdrantClient.ListCollectionsAsync(cancellationToken: ct);
            if (!collections.Contains(CollectionName))
            {
                await _qdrantClient.CreateCollectionAsync(
                    collectionName: CollectionName,
                    vectorsConfig: new VectorParams
                    {
                        Size = BgeM3Dimensions,
                        Distance = Distance.Cosine
                    },
                    cancellationToken: ct);

                _logger.LogInformation("Created Qdrant collection {CollectionName} with dimension {Dim}", CollectionName, BgeM3Dimensions);

                // Create payload indexes required for filtering
                await _qdrantClient.CreatePayloadIndexAsync(
                    collectionName: CollectionName,
                    fieldName: "materialId",
                    schemaType: PayloadSchemaType.Keyword,
                    cancellationToken: ct);

                await _qdrantClient.CreatePayloadIndexAsync(
                    collectionName: CollectionName,
                    fieldName: "chunkHash",
                    schemaType: PayloadSchemaType.Keyword,
                    cancellationToken: ct);

                await _qdrantClient.CreatePayloadIndexAsync(
                    collectionName: CollectionName,
                    fieldName: "materialVersionId",
                    schemaType: PayloadSchemaType.Keyword,
                    cancellationToken: ct);

                _logger.LogInformation("Created payload indexes for collection {CollectionName}", CollectionName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ensure Qdrant collection exists.");
            throw;
        }
    }

    private static Condition MatchCondition(string key, string value)
    {
        return new Condition
        {
            Field = new FieldCondition
            {
                Key = key,
                Match = new Match { Keyword = value }
            }
        };
    }

    private static Guid GenerateDeterministicPointId(Guid materialVersionId, int chunkIndex)
    {
        // Generate a deterministic UUID using MD5 hash of the string representation
        string input = $"{materialVersionId}_{chunkIndex}";
        using var md5 = MD5.Create();
        byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
        return new Guid(hash);
    }
}
