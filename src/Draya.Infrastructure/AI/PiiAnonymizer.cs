using System;
using System.Threading;
using System.Threading.Tasks;
using Draya.Application.Common.Interfaces;
using Draya.Domain.Identity;
using Draya.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Draya.Infrastructure.AI;

public class PiiAnonymizer : IPiiAnonymizer
{
    private readonly ApplicationDbContext _dbContext;

    public PiiAnonymizer(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Guid> GetAnonymizedIdAsync(Guid studentId, CancellationToken cancellationToken = default)
    {
        var mapping = await _dbContext.PiiMappings
            .FirstOrDefaultAsync(m => m.StudentId == studentId, cancellationToken);

        if (mapping != null)
        {
            return mapping.AnonymizedId;
        }

        var anonymizedId = Guid.NewGuid();
        mapping = new PiiMapping(studentId, anonymizedId);

        _dbContext.PiiMappings.Add(mapping);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return anonymizedId;
    }

    public async Task<Guid?> GetOriginalStudentIdAsync(Guid anonymizedId, CancellationToken cancellationToken = default)
    {
        var mapping = await _dbContext.PiiMappings
            .FirstOrDefaultAsync(m => m.AnonymizedId == anonymizedId, cancellationToken);

        return mapping?.StudentId;
    }
}
