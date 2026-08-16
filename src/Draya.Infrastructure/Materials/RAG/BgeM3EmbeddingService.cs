using System.Net.Http.Json;
using System.Text.Json;
using Draya.Application.Materials.RAG;
using Microsoft.Extensions.Logging;

namespace Draya.Infrastructure.Materials.RAG;

public class BgeM3EmbeddingService : IEmbeddingService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<BgeM3EmbeddingService> _logger;

    public BgeM3EmbeddingService(HttpClient httpClient, ILogger<BgeM3EmbeddingService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<float[]> EmbedAsync(string text, CancellationToken ct)
    {
        var embeddings = await EmbedBatchAsync(new List<string> { text }, ct);
        return embeddings.First();
    }

    public async Task<List<float[]>> EmbedBatchAsync(List<string> texts, CancellationToken ct)
    {
        if (texts == null || !texts.Any())
        {
            return new List<float[]>();
        }

        try
        {
            // HuggingFace Inference format: { "inputs": [ "...", "..." ] }
            var hfPayload = new { inputs = texts };
            
            // Post to base URL directly (e.g. HuggingFace router endpoint)
            var response = await _httpClient.PostAsJsonAsync("", hfPayload, ct);

            // If empty string / base URL didn't match (e.g. standard OpenAI endpoint expecting /v1/embeddings)
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                var openAiPayload = new { input = texts, model = "BAAI/bge-m3" };
                response = await _httpClient.PostAsJsonAsync("v1/embeddings", openAiPayload, ct);
            }

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(ct);
                _logger.LogError("Embedding API failed with status {StatusCode}: {ErrorContent}", response.StatusCode, errorContent);
                response.EnsureSuccessStatusCode();
            }

            var responseJson = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(responseJson);
            var root = doc.RootElement;

            var results = new List<float[]>();

            if (root.ValueKind == JsonValueKind.Array)
            {
                // HuggingFace direct array response: [[0.1, 0.2, ...], [0.3, ...]] or [0.1, 0.2, ...]
                if (root.GetArrayLength() > 0)
                {
                    var firstElem = root[0];
                    if (firstElem.ValueKind == JsonValueKind.Number)
                    {
                        // Single 1D embedding array: [0.1, 0.2, ...]
                        results.Add(ParseFloatArray(root));
                    }
                    else if (firstElem.ValueKind == JsonValueKind.Array)
                    {
                        // Array of arrays: [[...], [...]]
                        foreach (var item in root.EnumerateArray())
                        {
                            if (item.GetArrayLength() > 0 && item[0].ValueKind == JsonValueKind.Number)
                            {
                                results.Add(ParseFloatArray(item));
                            }
                            else if (item.GetArrayLength() > 0 && item[0].ValueKind == JsonValueKind.Array)
                            {
                                // 3D token embeddings [seq_len, dim] -> Mean pooling
                                results.Add(MeanPool(item));
                            }
                        }
                    }
                }
            }
            else if (root.ValueKind == JsonValueKind.Object)
            {
                // OpenAI-compatible response: { "data": [ { "index": 0, "embedding": [...] } ] }
                if (root.TryGetProperty("data", out var dataProp) && dataProp.ValueKind == JsonValueKind.Array)
                {
                    var items = new List<(int Index, float[] Embedding)>();
                    foreach (var item in dataProp.EnumerateArray())
                    {
                        int index = item.TryGetProperty("index", out var idxProp) ? idxProp.GetInt32() : items.Count;
                        if (item.TryGetProperty("embedding", out var embProp))
                        {
                            items.Add((index, ParseFloatArray(embProp)));
                        }
                    }
                    results = items.OrderBy(i => i.Index).Select(i => i.Embedding).ToList();
                }
            }

            if (results.Count != texts.Count)
            {
                _logger.LogWarning("Embedding API returned {ReceivedCount} embeddings for {RequestedCount} texts.", results.Count, texts.Count);
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate embeddings for batch of {Count} texts.", texts.Count);
            throw;
        }
    }

    private static float[] ParseFloatArray(JsonElement element)
    {
        var list = new float[element.GetArrayLength()];
        int i = 0;
        foreach (var val in element.EnumerateArray())
        {
            list[i++] = val.GetSingle();
        }
        return list;
    }

    private static float[] MeanPool(JsonElement tokenEmbeddings2D)
    {
        int tokenCount = tokenEmbeddings2D.GetArrayLength();
        if (tokenCount == 0) return Array.Empty<float>();

        int dim = tokenEmbeddings2D[0].GetArrayLength();
        var pooled = new float[dim];

        foreach (var token in tokenEmbeddings2D.EnumerateArray())
        {
            int d = 0;
            foreach (var val in token.EnumerateArray())
            {
                pooled[d++] += val.GetSingle();
            }
        }

        for (int d = 0; d < dim; d++)
        {
            pooled[d] /= tokenCount;
        }

        return pooled;
    }
}
