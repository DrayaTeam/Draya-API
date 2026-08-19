using System;
using System.Threading;
using System.Threading.Tasks;

namespace Draya.Domain.Exams;

public interface IExamGradingJobRepository
{
    Task<ExamGradingJob?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ExamGradingJob?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default);
    Task AddAsync(ExamGradingJob job, CancellationToken cancellationToken = default);
    Task UpdateAsync(ExamGradingJob job, CancellationToken cancellationToken = default);
}
