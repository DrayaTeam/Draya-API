using System.Collections.Generic;
using Draya.Application.AI.Models;

namespace Draya.Infrastructure.AI.Router;

public class AiRouterOptions
{
    public const string SectionName = "AiRouter";

    public ResilienceOptions Resilience { get; set; } = new();
    public Dictionary<string, List<string>> KeyPools { get; set; } = new();
    public Dictionary<AiFeature, FeatureConfig> Features { get; set; } = new();
}

public class ResilienceOptions
{
    public int MaxAttemptsPerRoute { get; set; } = 2;
    public int KeyCooldownSeconds { get; set; } = 60;
    public bool ProviderFallbackEnabled { get; set; } = true;
}

public class FeatureConfig
{
    public List<RouteConfig> Routes { get; set; } = new();
}

public class RouteConfig
{
    public string Provider { get; set; } = string.Empty;
    public string ModelId { get; set; } = string.Empty;
    public string KeyPool { get; set; } = string.Empty;
}
