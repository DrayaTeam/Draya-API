using Draya.Domain.Classrooms;
using Draya.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Draya.Infrastructure.Classrooms;

public class SectionRepository : ISectionRepository
{
    private readonly ApplicationDbContext _context;

    public SectionRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public IQueryable<ClassroomSection> GetQueryable()
    {
        return _context.ClassroomSections.AsQueryable();
    }

    public async Task<ClassroomSection?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ClassroomSections
            .Include(x => x.Materials)
                .ThenInclude(m => m.Versions)
            .Include(x => x.Materials)
                .ThenInclude(m => m.VideoDetail)
            .Include(x => x.Exams)
                .ThenInclude(e => e.Questions)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<ClassroomSection>> GetByClassroomIdAsync(Guid classroomId, CancellationToken cancellationToken = default)
    {
        return await _context.ClassroomSections
            .Include(x => x.Materials)
                .ThenInclude(m => m.Versions)
            .Include(x => x.Materials)
                .ThenInclude(m => m.VideoDetail)
            .Include(x => x.Exams)
                .ThenInclude(e => e.Questions)
            .Where(x => x.ClassroomId == classroomId)
            .OrderBy(x => x.Order).ThenBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(ClassroomSection section, CancellationToken cancellationToken = default)
    {
        await _context.ClassroomSections.AddAsync(section, cancellationToken);
    }

    public void Update(ClassroomSection section)
    {
        _context.ClassroomSections.Update(section);
    }

    public void Remove(ClassroomSection section)
    {
        _context.ClassroomSections.Remove(section);
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ClassroomSections.AnyAsync(x => x.Id == id, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
