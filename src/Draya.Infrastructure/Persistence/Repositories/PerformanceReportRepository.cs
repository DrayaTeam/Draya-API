using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Draya.Domain.Reports;
using Microsoft.EntityFrameworkCore;

namespace Draya.Infrastructure.Persistence.Repositories;

public class PerformanceReportRepository : IPerformanceReportRepository
{
    private readonly ApplicationDbContext _dbContext;

    public PerformanceReportRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(PerformanceReport report, CancellationToken cancellationToken = default)
    {
        await _dbContext.PerformanceReports.AddAsync(report, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<PerformanceReport?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.PerformanceReports.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<PerformanceReport?> GetLatestByStudentIdAsync(Guid studentId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.PerformanceReports
            .Where(x => x.StudentId == studentId)
            .OrderByDescending(x => x.GeneratedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task UpdateAsync(PerformanceReport report, CancellationToken cancellationToken = default)
    {
        // Explicitly mark IsApproved as modified so EF Core generates a targeted UPDATE.
        // Using the generic Update() can silently no-op when the entity has empty JSON-owned
        // collections (WeakTopics, SubjectProficiencies, TrendPoints = null in DB), because
        // EF Core's change tracker can't diff null JSON vs an empty C# list reliably.
        var entry = _dbContext.Entry(report);
        if (entry.State == Microsoft.EntityFrameworkCore.EntityState.Detached)
        {
            _dbContext.PerformanceReports.Attach(report);
        }
        entry.Property(r => r.IsApproved).IsModified = true;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public IQueryable<PerformanceReport> GetQueryable()
    {
        return _dbContext.PerformanceReports.AsQueryable();
    }
}
