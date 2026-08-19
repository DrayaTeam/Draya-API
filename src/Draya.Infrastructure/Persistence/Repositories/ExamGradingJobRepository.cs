using System;
using System.Threading;
using System.Threading.Tasks;
using Draya.Domain.Exams;
using Microsoft.EntityFrameworkCore;

namespace Draya.Infrastructure.Persistence.Repositories;

public class ExamGradingJobRepository : IExamGradingJobRepository
{
    private readonly ApplicationDbContext _context;

    public ExamGradingJobRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ExamGradingJob?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ExamGradingJobs.FirstOrDefaultAsync(j => j.Id == id, cancellationToken);
    }

    public async Task<ExamGradingJob?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default)
    {
        return await _context.ExamGradingJobs.FirstOrDefaultAsync(j => j.IdempotencyKey == idempotencyKey, cancellationToken);
    }

    public async Task AddAsync(ExamGradingJob job, CancellationToken cancellationToken = default)
    {
        await _context.ExamGradingJobs.AddAsync(job, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(ExamGradingJob job, CancellationToken cancellationToken = default)
    {
        // Private setters bypass EF Core's standard change detection.
        // Explicitly mark all mutable properties as Modified.
        var entry = _context.Entry(job);
        if (entry.State == EntityState.Detached)
            _context.ExamGradingJobs.Attach(job);

        entry.Property(x => x.Status).IsModified = true;
        entry.Property(x => x.CompletedAt).IsModified = true;
        entry.Property(x => x.ErrorMessage).IsModified = true;

        await _context.SaveChangesAsync(cancellationToken);
    }
}
