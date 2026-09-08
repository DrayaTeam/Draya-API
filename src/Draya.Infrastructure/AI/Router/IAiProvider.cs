using System.Threading;
using System.Threading.Tasks;
using Draya.Application.AI.Models;

namespace Draya.Infrastructure.AI.Router;

public interface IAiProvider
{
    string ProviderName { get; }
    
    Task<LlmResponse> GenerateAsync(
        LlmRequest request, 
        string modelId, 
        string apiKey, 
        CancellationToken cancellationToken = default);
}
