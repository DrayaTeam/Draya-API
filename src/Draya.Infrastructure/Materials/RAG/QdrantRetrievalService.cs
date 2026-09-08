using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Draya.Application.Materials.RAG;
using Microsoft.Extensions.Logging;
using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace Draya.Infrastructure.Materials.RAG;

public class QdrantRetrievalService : IRetrievalService
{
    private readonly QdrantClient _qdrantClient;
    private readonly IEmbeddingService _embeddingService;
    private readonly ILogger<QdrantRetrievalService> _logger;
    
    private const string CollectionName = "materials";

    public QdrantRetrievalService(
        QdrantClient qdrantClient, 
        IEmbeddingService embeddingService,
        ILogger<QdrantRetrievalService> logger)
    {
        _qdrantClient = qdrantClient;
        _embeddingService = embeddingService;
        _logger = logger;
    }

    public async Task<List<RetrievedChunk>> SearchAsync(RetrievalQuery query, CancellationToken ct = default)
    {
        // 1. Embed the query text
        var queryVector = await _embeddingService.EmbedAsync(query.QueryText, ct);

        // 2. Build the strict MaterialVersionIds filter
        var filter = new Filter();
        if (query.MaterialVersionIds != null && query.MaterialVersionIds.Any())
        {
            var shouldConditions = new List<Condition>();
            foreach (var id in query.MaterialVersionIds)
            {
                shouldConditions.Add(MatchCondition("materialVersionId", id.ToString()));
            }

            filter.Must.Add(new Condition
            {
                Filter = new Filter
                {
                    Should = { shouldConditions }
                }
            });
        }

        // 3. Search Qdrant
        var searchParams = new SearchParams { Exact = false, HnswEf = 128 };
        
        var response = await _qdrantClient.QueryAsync(
            collectionName: CollectionName,
            query: queryVector,
            filter: filter,
            limit: (ulong)query.TopK,
            scoreThreshold: query.MinScore,
            searchParams: searchParams,
            payloadSelector: true,
            cancellationToken: ct
        );

        // 4. Map the results
        var results = new List<RetrievedChunk>();
        
        foreach (var scoredPoint in response)
        {
            if (scoredPoint.Payload.TryGetValue("text", out var textVal))
            {
                results.Add(new RetrievedChunk
                {
                    ChunkId = scoredPoint.Id.Uuid,
                    Text = textVal.StringValue,
                    Score = scoredPoint.Score
                });
            }
        }

        return results;
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
}
