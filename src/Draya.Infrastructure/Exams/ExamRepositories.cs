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
        // The exam was loaded by GetByIdAsync so it's already tracked by EF.
        // We rely on EF's own change detection — no explicit Update() call needed
        // (calling Update() would force all child entities to Modified, which causes
        // a concurrency exception for newly Added options that have no existing row).
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Marks <paramref name="optionsToRemove"/> for deletion and adds
    /// <paramref name="newOptions"/> so that a subsequent <see cref="UpdateAsync"/>
    /// persists both the DELETE and the INSERT in one transaction.
    /// </summary>
    public void ReplaceOptionsForQuestion(
        IEnumerable<ExamQuestionOption> optionsToRemove,
        IEnumerable<ExamQuestionOption> newOptions)
    {
        // RemoveRange marks each entity as Deleted in the change tracker.
        // The options were already loaded by GetByIdAsync, so this is a
        // pure in-memory operation — zero extra DB round trips.
        _dbContext.ExamQuestionOptions.RemoveRange(optionsToRemove);

        // AddRange marks the new options as Added.
        _dbContext.ExamQuestionOptions.AddRange(newOptions);
    }

    public System.Linq.IQueryable<Exam> GetQueryable()
    {
        return _dbContext.Exams.AsQueryable();
    }
}
