using Microsoft.Extensions.Configuration;

namespace Draya.Infrastructure.AI.Router;

public class AiCredentialResolver : IAiCredentialResolver
{
    private readonly IConfiguration _configuration;

    public AiCredentialResolver(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string? ResolveSecret(string keyId)
    {
        return _configuration[$"AiCredentials:{keyId}"];
    }
}
