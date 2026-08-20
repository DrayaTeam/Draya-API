using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Draya.Domain.Exams;
using Microsoft.EntityFrameworkCore;

namespace Draya.Infrastructure.Persistence.Repositories;

public class StudentExamAttemptRepository : IStudentExamAttemptRepository
{
    private readonly ApplicationDbContext _context;

    public StudentExamAttemptRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<StudentExamAttempt?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.StudentExamAttempts
            .Include(x => x.Answers)
                .ThenInclude(a => a.GradingResult)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task AddAsync(StudentExamAttempt attempt, CancellationToken cancellationToken = default)
    {
        await _context.StudentExamAttempts.AddAsync(attempt, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(StudentExamAttempt attempt, CancellationToken cancellationToken = default)
    {
        // The entity was loaded via GetByIdAsync and is already tracked by this DbContext.
        // Calling .Update() would conflict with the tracked state, causing a concurrency exception.
        // We simply call SaveChangesAsync and let EF Core's change tracker handle the UPDATE + INSERT
        // for modified properties and any newly added child entities (StudentAnswers).
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task SubmitAsync(StudentExamAttempt attempt, List<StudentAnswer> answers, CancellationToken cancellationToken = default)
    {
        // Explicitly mark the submit-related properties as Modified.
        // This is required because private setters bypass EF Core's standard change detection.
        _context.Entry(attempt).Property(x => x.IsSubmitted).IsModified = true;
        _context.Entry(attempt).Property(x => x.SubmittedAt).IsModified = true;

        // Add the new answers directly to the DbSet (bypassing the aggregate's backing field
        // collection to avoid EF Core relationship tracking confusion).
        await _context.StudentAnswers.AddRangeAsync(answers, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveGradingResultsAsync(StudentExamAttempt attempt, List<AnswerGradingResult> results, CancellationToken cancellationToken = default)
    {
        // Add AnswerGradingResult entities directly to avoid EF Core backing-field tracking issues.
        // The AnswerGradingResult.StudentAnswerId FK is already set on each result.
        await _context.AnswerGradingResults.AddRangeAsync(results, cancellationToken);

        // Explicitly mark the grading-related properties on the attempt as Modified
        // since private setters bypass EF Core's standard change detection.
        _context.Entry(attempt).Property(x => x.FinalScore).IsModified = true;
        _context.Entry(attempt).Property(x => x.NeedsTeacherReview).IsModified = true;

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> GetCountByStudentAndExamAsync(Guid studentId, Guid examId, CancellationToken cancellationToken = default)
    {
        return await _context.StudentExamAttempts
            .CountAsync(x => x.StudentId == studentId && x.ExamId == examId, cancellationToken);
    }
}
