using System;
using System.Threading;
using System.Threading.Tasks;

namespace Draya.Application.Exams.Services;

public class StartGradingRequest
{
    public Guid StudentExamAttemptId { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
}

public interface IExamGradingService
{
    Task<Guid> StartGradingAsync(StartGradingRequest request, CancellationToken cancellationToken = default);
    Task ProcessGradingAsync(Guid gradingJobId, Guid studentExamAttemptId, CancellationToken cancellationToken = default);
}
