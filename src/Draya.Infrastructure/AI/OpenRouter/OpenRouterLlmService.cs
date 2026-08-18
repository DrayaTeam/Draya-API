using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Draya.Application.AI;
using Draya.Application.AI.Models;
using Microsoft.Extensions.Options;

namespace Draya.Infrastructure.AI.OpenRouter;

public class OpenRouterLlmService : ILLMService
{
    private readonly HttpClient _httpClient;
    private readonly OpenRouterOptions _options;

    public OpenRouterLlmService(HttpClient httpClient, IOptions<OpenRouterOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<LlmResponse> GenerateAsync(LlmRequest request, CancellationToken cancellationToken = default)
    {
        var openRouterRequest = new
        {
            model = _options.Model,
            messages = new[]
            {
                new { role = "system", content = request.SystemPrompt },
                new { role = "user", content = request.UserPrompt }
            },
            temperature = _options.Temperature,
            max_tokens = _options.MaxTokens,
            response_format = request.RequestJsonResponse ? new { type = "json_object" } : null
        };

        var response = await _httpClient.PostAsJsonAsync("chat/completions", openRouterRequest, cancellationToken);
        response.EnsureSuccessStatusCode();

        var openRouterResponse = await response.Content.ReadFromJsonAsync<OpenRouterChatResponse>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }, 
            cancellationToken);

        if (openRouterResponse == null || openRouterResponse.Choices.Length == 0)
        {
            throw new Exception("Received empty response from OpenRouter");
        }

        return new LlmResponse
        {
            Content = openRouterResponse.Choices[0].Message.Content,
            InputTokens = openRouterResponse.Usage?.PromptTokens,
            OutputTokens = openRouterResponse.Usage?.CompletionTokens
        };
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
