using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Draya.Application.AI.Models;
using Microsoft.Extensions.Logging;

namespace Draya.Infrastructure.AI.Router;

public class ItiProvider : IAiProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ItiProvider> _logger;

    public string ProviderName => "Iti";

    public ItiProvider(HttpClient httpClient, ILogger<ItiProvider> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<LlmResponse> GenerateAsync(LlmRequest request, string modelId, string apiKey, CancellationToken cancellationToken = default)
    {
        var correlationId = Guid.NewGuid().ToString("N");
        var payload = new
        {
            model_id = modelId,
            messages = new[] { new { role = "user", content = request.UserPrompt } },
            system_prompt = request.SystemPrompt
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "student/chat")
        {
            Content = JsonContent.Create(payload)
        };
        httpRequest.Headers.Add("Authorization", $"Bearer {apiKey}");

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw new AiProviderException(AiErrorType.TransientUnavailable, "Network error communicating with ITI.", null, correlationId, ex);
        }
        catch (TaskCanceledException ex)
        {
            throw new AiProviderException(AiErrorType.TransientUnavailable, "Request to ITI timed out.", null, correlationId, ex);
        }

        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<ItiChatResponse>(cancellationToken: cancellationToken);
            if (result == null)
            {
                throw new AiProviderException(AiErrorType.UnknownProviderFailure, "Received null response from ITI.", (int)response.StatusCode, correlationId);
            }
            return new LlmResponse
            {
                Content = result.Content ?? string.Empty,
                InputTokens = result.InputTokens,
                OutputTokens = result.OutputTokens
            };
        }

        var statusCode = (int)response.StatusCode;
        var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
        
        _logger.LogDebug("[{CorrelationId}] ITI Error {StatusCode}: {ErrorBody}", correlationId, statusCode, errorBody);

        var errorType = DetermineErrorType(statusCode, errorBody);
        
        throw new AiProviderException(
            errorType, 
            $"ITI API Error. Status: {statusCode}", 
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
            if (bodyLower.Contains("quota") || bodyLower.Contains("insufficient") || bodyLower.Contains("credit"))
            {
                return AiErrorType.QuotaExhausted;
            }
            if (bodyLower.Contains("rate"))
            {
                return AiErrorType.KeyRateLimited;
            }
            return AiErrorType.UnknownProviderFailure;
        }

        return AiErrorType.UnknownProviderFailure;
    }

    private class ItiChatResponse
    {
        [JsonPropertyName("content")]
        public string? Content { get; set; }
        
        [JsonPropertyName("input_tokens")]
        public int? InputTokens { get; set; }
        
        [JsonPropertyName("output_tokens")]
        public int? OutputTokens { get; set; }
    }
}
