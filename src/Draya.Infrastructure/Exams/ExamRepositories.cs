using System;
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
        _dbContext.Exams.Update(exam);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
