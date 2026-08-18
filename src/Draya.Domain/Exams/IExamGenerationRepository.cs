using System;
using System.Threading;
using System.Threading.Tasks;

namespace Draya.Domain.Exams;

public interface IExamGenerationRepository
{
    Task<ExamGeneration?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ExamGeneration?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default);
    Task AddAsync(ExamGeneration generation, CancellationToken cancellationToken = default);
    Task UpdateAsync(ExamGeneration generation, CancellationToken cancellationToken = default);
}
