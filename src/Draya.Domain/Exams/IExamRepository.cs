using System;
using System.Threading;
using System.Threading.Tasks;

namespace Draya.Domain.Exams;

public interface IExamRepository
{
    Task AddAsync(Exam exam, CancellationToken cancellationToken = default);
    Task<Exam?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
