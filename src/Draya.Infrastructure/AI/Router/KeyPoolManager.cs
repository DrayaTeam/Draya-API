using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Extensions.Options;

namespace Draya.Infrastructure.AI.Router;

public class ApiKeyState
{
    public string KeyId { get; set; } = string.Empty;
    public bool IsDisabled { get; set; }
    public DateTime? CooldownUntil { get; set; }
    public int FailureCount { get; set; }
    public DateTime? LastFailure { get; set; }
    
    public bool IsAvailable => !IsDisabled && (!CooldownUntil.HasValue || CooldownUntil.Value <= DateTime.UtcNow);
}

public class KeyPoolManager
{
    private readonly AiRouterOptions _options;
    private readonly ConcurrentDictionary<string, ApiKeyState> _keyStates = new();
    private readonly ConcurrentDictionary<string, int> _poolIndexes = new();

    public KeyPoolManager(IOptions<AiRouterOptions> options)
    {
        _options = options.Value;
        
        foreach (var pool in _options.KeyPools)
        {
            _poolIndexes[pool.Key] = 0;
            foreach (var keyId in pool.Value)
            {
                _keyStates.TryAdd(keyId, new ApiKeyState { KeyId = keyId });
            }
        }
    }

    public string? GetNextHealthyKeyId(string poolName)
    {
        if (!_options.KeyPools.TryGetValue(poolName, out var keys) || keys.Count == 0)
        {
            return null;
        }

        // Atomically increment and get the index to ensure round-robin across concurrent requests
        int startIndex = _poolIndexes.AddOrUpdate(poolName, 0, (k, current) => (current + 1) % keys.Count);
        
        for (int i = 0; i < keys.Count; i++)
        {
            int currentIndex = (startIndex + i) % keys.Count;
            var keyId = keys[currentIndex];

            if (_keyStates.TryGetValue(keyId, out var state) && state.IsAvailable)
            {
                return keyId;
            }
        }

        return null;
    }

    public void ReportFailure(string keyId, AiErrorType errorType)
    {
        if (!_keyStates.TryGetValue(keyId, out var state)) return;

        state.FailureCount++;
        state.LastFailure = DateTime.UtcNow;

        if (errorType == AiErrorType.InvalidCredential)
        {
            state.IsDisabled = true;
        }
        else if (errorType == AiErrorType.KeyRateLimited)
        {
            state.CooldownUntil = DateTime.UtcNow.AddSeconds(_options.Resilience.KeyCooldownSeconds);
        }
    }
}
