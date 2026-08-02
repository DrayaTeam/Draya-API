using Draya.Domain.Classrooms;
using Draya.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Draya.Infrastructure.Classrooms;

public class SubjectRepository : ISubjectRepository
{
    private readonly ApplicationDbContext _context;

    public SubjectRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Subject>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Subjects
            .OrderBy(s => s.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Subject?> GetByIdAsync(Guid subjectId, CancellationToken cancellationToken = default)
    {
        return await _context.Subjects
            .FirstOrDefaultAsync(s => s.Id == subjectId, cancellationToken);
    }

    public async Task<Subject?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var normalizedName = name.Trim().ToLowerInvariant();
        
        return await _context.Subjects
            .FirstOrDefaultAsync(s => s.Name.ToLower() == normalizedName, cancellationToken);
    }

    public async Task AddAsync(Subject subject, CancellationToken cancellationToken = default)
    {
        await _context.Subjects.AddAsync(subject, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
