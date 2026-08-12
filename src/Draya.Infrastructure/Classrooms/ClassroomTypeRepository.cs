using Draya.Domain.Classrooms;
using Draya.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Draya.Infrastructure.Classrooms;

public class ClassroomTypeRepository : IClassroomTypeRepository
{
    private readonly ApplicationDbContext _context;
    public ClassroomTypeRepository(ApplicationDbContext context) => _context = context;

    public async Task<ClassroomType?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.ClassroomTypes.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<List<ClassroomType>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _context.ClassroomTypes.ToListAsync(cancellationToken);

    public async Task AddAsync(ClassroomType entity, CancellationToken cancellationToken = default)
        => await _context.ClassroomTypes.AddAsync(entity, cancellationToken);

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => await _context.SaveChangesAsync(cancellationToken);
}
