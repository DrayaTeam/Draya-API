using Draya.Domain.Identity;
using Draya.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Draya.Infrastructure.Identity.Repositories;

public class TeacherRepository : ITeacherRepository
{
    private readonly ApplicationDbContext _context;

    public TeacherRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Teacher?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        => await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == userId, cancellationToken);

    public async Task<List<Teacher>> GetByUserIdsAsync(IEnumerable<Guid> userIds, CancellationToken cancellationToken = default)
        => await _context.Teachers.Where(t => userIds.Contains(t.UserId)).ToListAsync(cancellationToken);

    public async Task<IEnumerable<Teacher>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _context.Teachers.ToListAsync(cancellationToken);

    public async Task AddAsync(Teacher teacher, CancellationToken cancellationToken = default)
        => await _context.Teachers.AddAsync(teacher, cancellationToken);

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => await _context.SaveChangesAsync(cancellationToken);
}
