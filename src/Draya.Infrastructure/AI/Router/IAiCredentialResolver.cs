namespace Draya.Infrastructure.AI.Router;

public interface IAiCredentialResolver
{
    string? ResolveSecret(string keyId);
}
