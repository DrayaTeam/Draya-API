using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Draya.Domain.Exams;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Draya.Infrastructure.Persistence.Repositories;

public class StudentExamAttemptRepository : IStudentExamAttemptRepository
{
    private readonly ApplicationDbContext _context;

    public StudentExamAttemptRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public IQueryable<StudentExamAttempt> GetQueryable()
    {
        return _context.StudentExamAttempts.AsQueryable();
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
        // Explicitly mark the submit-related and score properties as Modified.
        _context.Entry(attempt).Property(x => x.IsSubmitted).IsModified = true;
        _context.Entry(attempt).Property(x => x.SubmittedAt).IsModified = true;
        _context.Entry(attempt).Property(x => x.FinalScore).IsModified = true;
        _context.Entry(attempt).Property(x => x.MaxScore).IsModified = true;
        _context.Entry(attempt).Property(x => x.NeedsTeacherReview).IsModified = true;

        // If the student already has answers saved (e.g., from autosave), remove them 
        // to prevent a PK/Unique key collision when adding the final submitted answers.
        var existingAnswers = await _context.StudentAnswers
            .Where(a => a.StudentExamAttemptId == attempt.Id)
            .ToListAsync(cancellationToken);
            
        if (existingAnswers.Any())
        {
            _context.StudentAnswers.RemoveRange(existingAnswers);
        }

        // Add the new final answers directly to the DbSet.
        await _context.StudentAnswers.AddRangeAsync(answers, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveGradingResultsAsync(StudentExamAttempt attempt, List<AnswerGradingResult> results, CancellationToken cancellationToken = default)
    {
        // The AnswerGradingResult entities are explicitly added here (typically by the background grading job)
        // since they aren't part of a Submit cascade.
        await _context.AnswerGradingResults.AddRangeAsync(results, cancellationToken);

        // Explicitly mark the grading-related properties on the attempt as Modified
        // since private setters bypass EF Core's standard change detection.
        _context.Entry(attempt).Property(x => x.FinalScore).IsModified = true;
        _context.Entry(attempt).Property(x => x.MaxScore).IsModified = true;
        _context.Entry(attempt).Property(x => x.NeedsTeacherReview).IsModified = true;

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> GetCountByStudentAndExamAsync(Guid studentId, Guid examId, CancellationToken cancellationToken = default)
    {
        return await _context.StudentExamAttempts
            .CountAsync(x => x.StudentId == studentId && x.ExamId == examId, cancellationToken);
    }

    public async Task<StudentExamAttempt?> GetActiveAttemptAsync(Guid studentId, Guid examId, CancellationToken cancellationToken = default)
    {
        return await _context.StudentExamAttempts
            .FirstOrDefaultAsync(x => x.StudentId == studentId && x.ExamId == examId && !x.IsSubmitted, cancellationToken);
    }

    public async Task<List<StudentExamAttempt>> GetAttemptsByStudentAndExamsAsync(Guid studentId, IEnumerable<Guid> examIds, CancellationToken cancellationToken = default)
    {
        return await _context.StudentExamAttempts
            .Where(x => x.StudentId == studentId && examIds.Contains(x.ExamId))
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> OverrideAnswerScoreAsync(Guid attemptId, Guid answerId, decimal newScore, Guid teacherId, CancellationToken cancellationToken = default)
    {
        var attempt = await _context.StudentExamAttempts
            .Include(a => a.Answers)
                .ThenInclude(a => a.GradingResult)
            .FirstOrDefaultAsync(a => a.Id == attemptId, cancellationToken);

        if (attempt == null) return false;

        var answer = attempt.Answers.FirstOrDefault(a => a.Id == answerId);
        if (answer == null || answer.GradingResult == null) return false;

        IDbContextTransaction? transaction = _context.Database.IsRelational() 
            ? await _context.Database.BeginTransactionAsync(cancellationToken) 
            : null;

        try
        {
            answer.GradingResult.OverrideScore(newScore, teacherId);

            decimal totalScore = attempt.Answers.Sum(a => a.GradingResult?.GetFinalScore() ?? 0m);
            decimal totalMaxScore = attempt.Answers.Sum(a => Math.Max(a.GradingResult?.MaxScore ?? 1, 1));
            bool stillNeedsReview = attempt.Answers.Any(a => a.GradingResult?.NeedsTeacherReview == true && a.GradingResult?.IsFinalized == false);

            attempt.UpdateFinalScore(totalScore, totalMaxScore, stillNeedsReview);
            _context.StudentExamAttempts.Update(attempt);

            if (!stillNeedsReview)
            {
                await FinalizeAttemptAndWeaknessesInternalAsync(attempt, cancellationToken);
            }

            await _context.SaveChangesAsync(cancellationToken);
            if (transaction != null)
            {
                await transaction.CommitAsync(cancellationToken);
            }
            return true;
        }
        catch
        {
            if (transaction != null)
            {
                await transaction.RollbackAsync(cancellationToken);
            }
            return false;
        }
    }

    public async Task<bool> FinalizeAttemptAndWeaknessesAsync(Guid attemptId, CancellationToken cancellationToken = default)
    {
        var attempt = await _context.StudentExamAttempts
            .Include(a => a.Answers)
                .ThenInclude(a => a.GradingResult)
            .FirstOrDefaultAsync(a => a.Id == attemptId, cancellationToken);

        if (attempt == null) return false;

        if (attempt.Answers.Any(a => a.GradingResult?.NeedsTeacherReview == true && a.GradingResult?.IsFinalized == false))
        {
            return false;
        }

        IDbContextTransaction? transaction = _context.Database.IsRelational() 
            ? await _context.Database.BeginTransactionAsync(cancellationToken) 
            : null;

        try
        {
            await FinalizeAttemptAndWeaknessesInternalAsync(attempt, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            if (transaction != null)
            {
                await transaction.CommitAsync(cancellationToken);
            }
            return true;
        }
        catch
        {
            if (transaction != null)
            {
                await transaction.RollbackAsync(cancellationToken);
            }
            return false;
        }
    }

    private async Task FinalizeAttemptAndWeaknessesInternalAsync(StudentExamAttempt attempt, CancellationToken cancellationToken)
    {
        var exam = await _context.Exams.FirstOrDefaultAsync(e => e.Id == attempt.ExamId, cancellationToken);
        if (exam == null) return;

        var studentId = attempt.StudentId;
        var topicName = exam.Topic;
        var topicId = Draya.Application.Utils.GuidUtility.Create(Draya.Application.Utils.GuidUtility.IsoOidNamespace, topicName ?? "General");

        var weakness = await _context.StudentWeaknesses
            .FirstOrDefaultAsync(w => w.StudentId == studentId && w.TopicId == topicId, cancellationToken);

        decimal totalScore = attempt.FinalScore ?? 0m;
        decimal maxScore = attempt.MaxScore ?? attempt.Answers.Sum(a => Math.Max(a.GradingResult?.MaxScore ?? 1, 1));
        decimal proficiency = totalScore / (maxScore > 0 ? maxScore : 1) * 100m;
        decimal masteryThreshold = 85m;

        bool previousIsActive = true;
        decimal previousProficiency = 0m;

        if (weakness == null)
        {
            weakness = new Draya.Domain.Reports.StudentWeakness(studentId, topicId, topicName, proficiency);
            await _context.StudentWeaknesses.AddAsync(weakness, cancellationToken);
        }
        else
        {
            previousIsActive = weakness.IsActive;
            previousProficiency = weakness.CurrentProficiencyPercent;
            weakness.UpdatePerformance(proficiency, masteryThreshold, false);
        }

        var history = new Draya.Domain.Reports.StudentWeaknessHistory(
            weakness.Id,
            previousProficiency,
            weakness.CurrentProficiencyPercent,
            previousIsActive,
            weakness.IsActive,
            attempt.Id
        );
        
        await _context.StudentWeaknessHistories.AddAsync(history, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken); // Save to get weakness Id if new

        var delta = Math.Abs(weakness.CurrentProficiencyPercent - previousProficiency);
        if (delta >= 10m || weakness.IsActive != previousIsActive)
        {
            var activeReview = await _context.WeaknessReviews
                .FirstOrDefaultAsync(r => r.StudentWeaknessId == weakness.Id && !r.IsOutdated, cancellationToken);
            if (activeReview != null)
            {
                activeReview.MarkOutdated();
            }
        }
    }
}
