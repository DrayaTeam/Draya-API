using System;
using System.Threading;
using System.Threading.Tasks;

namespace Draya.Application.Reports.Services;

public interface IReportGenerationService
{
    Task GenerateReportAsync(Guid studentId, Guid examAttemptId, CancellationToken cancellationToken = default);
}
