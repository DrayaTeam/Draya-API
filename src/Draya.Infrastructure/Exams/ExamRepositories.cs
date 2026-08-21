using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Draya.Domain.Exams;
using Draya.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Draya.Infrastructure.Exams;

public class ExamGenerationRepository : IExamGenerationRepository
{
    private readonly ApplicationDbContext _dbContext;

    public ExamGenerationRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ExamGeneration?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ExamGenerations.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<ExamGeneration?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ExamGenerations
            .FirstOrDefaultAsync(e => e.IdempotencyKey == idempotencyKey, cancellationToken);
    }

    public async Task AddAsync(ExamGeneration generation, CancellationToken cancellationToken = default)
    {
        _dbContext.ExamGenerations.Add(generation);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(ExamGeneration generation, CancellationToken cancellationToken = default)
    {
        _dbContext.ExamGenerations.Update(generation);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}

public class ExamRepository : IExamRepository
{
    private readonly ApplicationDbContext _dbContext;

    public ExamRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Exam exam, CancellationToken cancellationToken = default)
    {
        _dbContext.Exams.Add(exam);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Exam?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Exams
            .Include(e => e.Questions)
            .ThenInclude(q => q.Options)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task UpdateAsync(Exam exam, CancellationToken cancellationToken = default)
    {
        // We only call Update if the entity is detached. Since it's loaded via GetByIdAsync,
        // it's already tracked. Calling Update() forces all entities with non-default keys to Modified,
        // which causes ConcurrencyExceptions for newly added Options.
        if (_dbContext.Entry(exam).State == EntityState.Detached)
        {
            _dbContext.Exams.Update(exam);
        }
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task RemoveOptionsForQuestionAsync(Guid questionId, CancellationToken cancellationToken = default)
    {
        // ExecuteDeleteAsync issues a direct DELETE SQL, bypassing the EF change tracker.
        // However, the old ExamQuestionOption entities are still tracked as Unchanged in
        // the current DbContext session (they were loaded by GetByIdAsync). If we leave them
        // tracked, the next SaveChangesAsync will try to UPDATE/process those now-deleted rows,
        // causing a DbUpdateConcurrencyException (0 rows affected).
        // Fix: detach all tracked options for this question BEFORE issuing the DELETE so EF
        // forgets about them entirely.
        var trackedOptions = _dbContext.ChangeTracker
            .Entries<ExamQuestionOption>()
            .Where(e => e.Entity.ExamQuestionId == questionId)
            .ToList();

        foreach (var entry in trackedOptions)
            entry.State = EntityState.Detached;

        await _dbContext.ExamQuestionOptions
            .Where(o => o.ExamQuestionId == questionId)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public System.Linq.IQueryable<Exam> GetQueryable()
    {
        return _dbContext.Exams.AsQueryable();
    }
}
