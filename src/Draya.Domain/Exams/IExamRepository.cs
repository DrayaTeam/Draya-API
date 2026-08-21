using System;
using System.Threading;
using System.Threading.Tasks;

namespace Draya.Domain.Exams;

public interface IExamRepository
{
    Task AddAsync(Exam exam, CancellationToken cancellationToken = default);
    Task UpdateAsync(Exam exam, CancellationToken cancellationToken = default);
    Task<Exam?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    System.Linq.IQueryable<Exam> GetQueryable();
    /// <summary>
    /// Explicitly removes all persisted options for a question from the database
    /// so that EF Core does not leave orphaned rows when options are replaced.
    /// </summary>
    Task RemoveOptionsForQuestionAsync(Guid questionId, CancellationToken cancellationToken = default);
}
