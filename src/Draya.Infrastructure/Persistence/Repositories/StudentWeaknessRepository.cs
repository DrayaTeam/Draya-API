using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Draya.Domain.Reports;
using Microsoft.EntityFrameworkCore;

namespace Draya.Infrastructure.Persistence.Repositories;

public class StudentWeaknessRepository : IStudentWeaknessRepository
{
    private readonly ApplicationDbContext _dbContext;

    public StudentWeaknessRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<StudentWeakness?> GetActiveByTopicAsync(Guid studentId, string topicName, CancellationToken cancellationToken = default)
    {
        return await _dbContext.StudentWeaknesses
            .FirstOrDefaultAsync(w => w.StudentId == studentId && w.TopicNameSnapshot == topicName && w.IsActive, cancellationToken);
    }

    public async Task<List<StudentWeaknessHistory>> GetHistoryAsync(Guid weaknessId, int take = 2, CancellationToken cancellationToken = default)
    {
        return await _dbContext.StudentWeaknessHistories
            .Where(h => h.StudentWeaknessId == weaknessId)
            .OrderByDescending(h => h.CreatedAt)
            .Take(take)
            .ToListAsync(cancellationToken);
    }
}
