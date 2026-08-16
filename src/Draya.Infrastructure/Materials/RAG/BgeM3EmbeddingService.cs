using System.Net.Http.Json;
using System.Text.Json.Serialization;
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
            // Assuming standard OpenAI-compatible embeddings endpoint (e.g., TEI, Ollama, vLLM)
            var request = new EmbeddingRequest
            {
                Input = texts,
                Model = "BAAI/bge-m3" 
            };

            var response = await _httpClient.PostAsJsonAsync("v1/embeddings", request, ct);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(ct);
                _logger.LogError("Embedding API failed with status {StatusCode}: {ErrorContent}", response.StatusCode, errorContent);
                response.EnsureSuccessStatusCode();
            }

            var result = await response.Content.ReadFromJsonAsync<EmbeddingResponse>(cancellationToken: ct);

            if (result == null || result.Data == null)
            {
                throw new InvalidOperationException("Embedding API returned empty or invalid data.");
            }

            // Ensure they are returned in the exact order requested
            return result.Data
                .OrderBy(d => d.Index)
                .Select(d => d.Embedding)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate embeddings for batch of {Count} texts.", texts.Count);
            throw;
        }
    }

    private class EmbeddingRequest
    {
        [JsonPropertyName("input")]
        public List<string> Input { get; set; } = new();

        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;
    }

    private class EmbeddingResponse
    {
        [JsonPropertyName("data")]
        public List<EmbeddingData> Data { get; set; } = new();
    }

    private class EmbeddingData
    {
        [JsonPropertyName("index")]
        public int Index { get; set; }

        [JsonPropertyName("embedding")]
        public float[] Embedding { get; set; } = Array.Empty<float>();
    }
}
