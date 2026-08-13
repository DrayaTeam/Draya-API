using Draya.Domain.Classrooms;
using Draya.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Draya.Infrastructure.Classrooms;

public class ClassroomRepository : IClassroomRepository
{
    private readonly ApplicationDbContext _context;

    public ClassroomRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Classroom?> GetByIdAsync(Guid classroomId, CancellationToken cancellationToken = default)
    {
        return await _context.Classrooms
            .Include(c => c.Subject)
            .Include(c => c.ClassroomType)
            .Include(c => c.GradeLevel)
            .FirstOrDefaultAsync(c => c.Id == classroomId, cancellationToken);
    }

    public async Task<Classroom?> GetByEnrollmentCodeAsync(string enrollmentCode, CancellationToken cancellationToken = default)
    {
        return await _context.Classrooms
            .Include(c => c.Subject)
            .Include(c => c.ClassroomType)
            .Include(c => c.GradeLevel)
            .FirstOrDefaultAsync(c => c.EnrollmentCode == enrollmentCode, cancellationToken);
    }

    public async Task<List<Classroom>> GetByTeacherIdAsync(Guid teacherId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        return await _context.Classrooms
            .Include(c => c.Subject)
            .Include(c => c.ClassroomType)
            .Include(c => c.GradeLevel)
            .Where(c => c.TeacherId == teacherId)
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetCountByTeacherIdAsync(Guid teacherId, CancellationToken cancellationToken = default)
    {
        return await _context.Classrooms
            .CountAsync(c => c.TeacherId == teacherId, cancellationToken);
    }

    public async Task<List<Classroom>> GetByStudentIdAsync(Guid studentId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        return await _context.Classrooms
            .Include(c => c.Subject)
            .Include(c => c.ClassroomType)
            .Include(c => c.GradeLevel)
            .Where(c => _context.Enrollments.Any(e => 
                e.StudentId == studentId && 
                e.ClassroomId == c.Id && 
                e.Status == EnrollmentStatus.Active))
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetCountByStudentIdAsync(Guid studentId, CancellationToken cancellationToken = default)
    {
        return await _context.Classrooms
            .CountAsync(c => _context.Enrollments.Any(e => 
                e.StudentId == studentId && 
                e.ClassroomId == c.Id && 
                e.Status == EnrollmentStatus.Active), cancellationToken);
    }

    public async Task<bool> IsStudentEnrolledAsync(Guid studentId, Guid classroomId, CancellationToken cancellationToken = default)
    {
        return await _context.Enrollments
            .AnyAsync(e => 
                e.StudentId == studentId && 
                e.ClassroomId == classroomId && 
                e.Status == EnrollmentStatus.Active, 
                cancellationToken);
    }

    public async Task<List<Guid>> GetEnrolledClassroomIdsAsync(Guid studentId, CancellationToken cancellationToken = default)
    {
        return await _context.Enrollments
            .Where(e => e.StudentId == studentId && e.Status == EnrollmentStatus.Active)
            .Select(e => e.ClassroomId)
            .ToListAsync(cancellationToken);
    }

    public IQueryable<Classroom> GetQueryable()
    {
        return _context.Classrooms
            .Include(c => c.Subject)
            .Include(c => c.ClassroomType)
            .Include(c => c.GradeLevel)
            .AsQueryable();
    }

    public async Task AddAsync(Classroom classroom, CancellationToken cancellationToken = default)
    {
        await _context.Classrooms.AddAsync(classroom, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
