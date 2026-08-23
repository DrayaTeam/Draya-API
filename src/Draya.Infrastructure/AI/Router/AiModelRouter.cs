using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Draya.Application.AI;
using Draya.Application.AI.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Draya.Infrastructure.AI.Router;

public class AiModelRouter : ILLMService
{
    private readonly AiRouterOptions _options;
    private readonly KeyPoolManager _keyPoolManager;
    private readonly IAiCredentialResolver _credentialResolver;
    private readonly IEnumerable<IAiProvider> _providers;
    private readonly ILogger<AiModelRouter> _logger;

    public AiModelRouter(
        IOptions<AiRouterOptions> options,
        KeyPoolManager keyPoolManager,
        IAiCredentialResolver credentialResolver,
        IEnumerable<IAiProvider> providers,
        ILogger<AiModelRouter> logger)
    {
        _options = options.Value;
        _keyPoolManager = keyPoolManager;
        _credentialResolver = credentialResolver;
        _providers = providers;
        _logger = logger;
    }

    public async Task<LlmResponse> GenerateAsync(LlmRequest request, CancellationToken cancellationToken = default)
    {
        if (!_options.Features.TryGetValue(request.Feature, out var featureConfig) || featureConfig.Routes.Count == 0)
        {
            throw new InvalidOperationException($"No routes configured for AI feature: {request.Feature}");
        }

        var maxAttempts = _options.Resilience.MaxAttemptsPerRoute;

        foreach (var route in featureConfig.Routes)
        {
            var provider = _providers.FirstOrDefault(p => p.ProviderName.Equals(route.Provider, StringComparison.OrdinalIgnoreCase));
            if (provider == null)
            {
                _logger.LogWarning("Configured provider '{Provider}' for feature {Feature} was not found.", route.Provider, request.Feature);
                continue;
            }

            int attempt = 0;
            while (attempt < maxAttempts)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var keyId = _keyPoolManager.GetNextHealthyKeyId(route.KeyPool);
                if (keyId == null)
                {
                    _logger.LogWarning("No healthy keys available in pool '{Pool}' for route {Provider}/{Model}.", route.KeyPool, route.Provider, route.ModelId);
                    break; // Break while loop to move to the next route
                }

                var secret = _credentialResolver.ResolveSecret(keyId);
                if (string.IsNullOrWhiteSpace(secret))
                {
                    _logger.LogError("Secret not found for KeyId '{KeyId}'. Disabling key.", keyId);
                    _keyPoolManager.ReportFailure(keyId, AiErrorType.InvalidCredential);
                    attempt++;
                    continue;
                }

                try
                {
                    _logger.LogDebug("Attempting request for {Feature} via {Provider}/{Model} using key {KeyId} (Attempt {Attempt}/{Max})", 
                        request.Feature, route.Provider, route.ModelId, keyId, attempt + 1, maxAttempts);

                    return await provider.GenerateAsync(request, route.ModelId, secret, cancellationToken);
                }
                catch (AiProviderException ex)
                {
                    _logger.LogWarning(ex, "[{CorrelationId}] Provider exception on {Provider}/{Model}: {ErrorType}", 
                        ex.CorrelationId, route.Provider, route.ModelId, ex.ErrorType);

                    if (ex.ErrorType == AiErrorType.InvalidRequest)
                    {
                        throw; 
                    }

                    if (ex.ErrorType == AiErrorType.KeyRateLimited || ex.ErrorType == AiErrorType.InvalidCredential)
                    {
                        _keyPoolManager.ReportFailure(keyId, ex.ErrorType);
                        attempt++;
                        continue;
                    }

                    if (ex.ErrorType == AiErrorType.TransientUnavailable || ex.ErrorType == AiErrorType.UnknownProviderFailure)
                    {
                        attempt++;
                        if (attempt < maxAttempts)
                        {
                            var delayMs = Math.Min(5000, Math.Pow(2, attempt) * 1000);
                            _logger.LogInformation("Transient failure. Backing off for {DelayMs}ms before retrying.", delayMs);
                            await Task.Delay((int)delayMs, cancellationToken);
                        }
                        continue;
                    }

                    if (ex.ErrorType == AiErrorType.ProviderRateLimited || ex.ErrorType == AiErrorType.QuotaExhausted)
                    {
                        _keyPoolManager.ReportFailure(keyId, ex.ErrorType);
                        _logger.LogWarning("Provider or Quota exhausted. Skipping remaining attempts on route {Provider}.", route.Provider);
                        break; // Move to the next route
                    }
                }
            }

            if (!_options.Resilience.ProviderFallbackEnabled)
            {
                break;
            }
        }

        throw new Exception($"All AI routes failed or were exhausted for feature: {request.Feature}");
    }
}
