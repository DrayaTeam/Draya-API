using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Draya.Domain.Exams;

public interface IStudentExamAttemptRepository
{
    Task<StudentExamAttempt?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(StudentExamAttempt attempt, CancellationToken cancellationToken = default);
    Task UpdateAsync(StudentExamAttempt attempt, CancellationToken cancellationToken = default);
    Task SubmitAsync(StudentExamAttempt attempt, List<StudentAnswer> answers, CancellationToken cancellationToken = default);
    Task SaveGradingResultsAsync(StudentExamAttempt attempt, List<AnswerGradingResult> results, CancellationToken cancellationToken = default);
    Task<int> GetCountByStudentAndExamAsync(Guid studentId, Guid examId, CancellationToken cancellationToken = default);
    Task<StudentExamAttempt?> GetActiveAttemptAsync(Guid studentId, Guid examId, CancellationToken cancellationToken = default);
    Task<List<StudentExamAttempt>> GetAttemptsByStudentAndExamsAsync(Guid studentId, IEnumerable<Guid> examIds, CancellationToken cancellationToken = default);
}
