using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Draya.Application.AI.Models;
using Microsoft.Extensions.Logging;

namespace Draya.Infrastructure.AI.Router;

public class OpenRouterProvider : IAiProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OpenRouterProvider> _logger;

    public string ProviderName => "OpenRouter";

    public OpenRouterProvider(HttpClient httpClient, ILogger<OpenRouterProvider> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<LlmResponse> GenerateAsync(LlmRequest request, string modelId, string apiKey, CancellationToken cancellationToken = default)
    {
        var correlationId = Guid.NewGuid().ToString("N");
        
        var openRouterRequest = new
        {
            model = modelId,
            messages = new[]
            {
                new { role = "system", content = request.SystemPrompt },
                new { role = "user", content = request.UserPrompt }
            },
            temperature = 0.0,
            max_tokens = 10000,
            response_format = request.RequestJsonResponse ? new { type = "json_object" } : null
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "chat/completions")
        {
            Content = JsonContent.Create(openRouterRequest)
        };
        httpRequest.Headers.Add("Authorization", $"Bearer {apiKey}");
        httpRequest.Headers.Add("HTTP-Referer", "https://draya.com"); 

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        }
        catch (Exception ex)
        {
            throw new AiProviderException(AiErrorType.TransientUnavailable, "Network error communicating with OpenRouter.", null, correlationId, ex);
        }

        if (response.IsSuccessStatusCode)
        {
            var openRouterResponse = await response.Content.ReadFromJsonAsync<OpenRouterChatResponse>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }, 
                cancellationToken);

            if (openRouterResponse == null || openRouterResponse.Choices == null || openRouterResponse.Choices.Length == 0)
            {
                throw new AiProviderException(AiErrorType.UnknownProviderFailure, "Received empty response from OpenRouter.", (int)response.StatusCode, correlationId);
            }

            return new LlmResponse
            {
                Content = openRouterResponse.Choices[0].Message.Content,
                InputTokens = openRouterResponse.Usage?.PromptTokens,
                OutputTokens = openRouterResponse.Usage?.CompletionTokens
            };
        }

        var statusCode = (int)response.StatusCode;
        var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
        
        _logger.LogDebug("[{CorrelationId}] OpenRouter Error {StatusCode}: {ErrorBody}", correlationId, statusCode, errorBody);

        var errorType = DetermineErrorType(statusCode, errorBody);
        
        throw new AiProviderException(
            errorType, 
            $"OpenRouter API Error. Status: {statusCode}", 
            statusCode, 
            correlationId);
    }

    private AiErrorType DetermineErrorType(int statusCode, string errorBody)
    {
        if (statusCode == 401 || statusCode == 403)
            return AiErrorType.InvalidCredential;
            
        if (statusCode == 400 || statusCode == 422)
            return AiErrorType.InvalidRequest;

        if (statusCode >= 500)
            return AiErrorType.TransientUnavailable;

        if (statusCode == 429)
        {
            var bodyLower = errorBody.ToLowerInvariant();
            if (bodyLower.Contains("quota") || bodyLower.Contains("credit"))
                return AiErrorType.QuotaExhausted;
                
            return AiErrorType.KeyRateLimited;
        }
        
        if (statusCode == 402)
            return AiErrorType.QuotaExhausted;

        return AiErrorType.UnknownProviderFailure;
    }

    private class OpenRouterChatResponse
    {
        public Choice[] Choices { get; set; } = Array.Empty<Choice>();
        public UsageInfo? Usage { get; set; }
    }

    private class Choice
    {
        public Message Message { get; set; } = new Message();
    }

    private class Message
    {
        public string Content { get; set; } = string.Empty;
    }

    private class UsageInfo
    {
        [JsonPropertyName("prompt_tokens")]
        public int PromptTokens { get; set; }

        [JsonPropertyName("completion_tokens")]
        public int CompletionTokens { get; set; }
    }
}
