using Draya.Domain.Classrooms;
using Draya.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Draya.Infrastructure.Classrooms;

public class GradeLevelRepository : IGradeLevelRepository
{
    private readonly ApplicationDbContext _context;
    public GradeLevelRepository(ApplicationDbContext context) => _context = context;

    public async Task<GradeLevel?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.GradeLevels.FirstOrDefaultAsync(g => g.Id == id, cancellationToken);

    public async Task<List<GradeLevel>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _context.GradeLevels.OrderBy(g => g.SortOrder).ToListAsync(cancellationToken);

    public async Task AddAsync(GradeLevel entity, CancellationToken cancellationToken = default)
        => await _context.GradeLevels.AddAsync(entity, cancellationToken);

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => await _context.SaveChangesAsync(cancellationToken);
}
