using System.Threading;
using System.Threading.Tasks;
using Draya.Application.AI.Models;

namespace Draya.Application.AI;

public interface ILLMService
{
    Task<LlmResponse> GenerateAsync(LlmRequest request, CancellationToken cancellationToken = default);
}
