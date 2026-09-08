namespace Draya.Infrastructure.AI.OpenRouter;

public class OpenRouterOptions
{
    public const string SectionName = "OpenRouter";
    
    public string BaseUrl { get; set; } = "https://openrouter.ai/api/v1";
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "nvidia/nemotron-3-super-120b-a12b:free";
    public double Temperature { get; set; } = 0.0;
    public int MaxTokens { get; set; } = 3000;
}
