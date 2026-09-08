using System;
using System.Collections.Generic;
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
    /// Stages the replacement of a question's options in the EF change tracker.
    /// Old options are marked Deleted; new options are marked Added.
    /// Call <see cref="UpdateAsync"/> afterwards to persist both in one transaction.
    /// </summary>
    void ReplaceOptionsForQuestion(
        IEnumerable<ExamQuestionOption> optionsToRemove,
        IEnumerable<ExamQuestionOption> newOptions);
}
