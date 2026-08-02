using Draya.Domain.Classrooms;
using Draya.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Draya.Infrastructure.Classrooms;

public class EnrollmentRepository : IEnrollmentRepository
{
    private readonly ApplicationDbContext _context;

    public EnrollmentRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Enrollment?> GetByStudentAndClassroomAsync(Guid studentId, Guid classroomId, CancellationToken cancellationToken = default)
    {
        return await _context.Enrollments
            .FirstOrDefaultAsync(e => 
                e.StudentId == studentId && 
                e.ClassroomId == classroomId, 
                cancellationToken);
    }

    public async Task<List<Enrollment>> GetByClassroomIdAsync(Guid classroomId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        return await _context.Enrollments
            .Where(e => e.ClassroomId == classroomId && e.Status == EnrollmentStatus.Active)
            .OrderByDescending(e => e.EnrolledAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetCountByClassroomIdAsync(Guid classroomId, CancellationToken cancellationToken = default)
    {
        return await _context.Enrollments
            .CountAsync(e => e.ClassroomId == classroomId && e.Status == EnrollmentStatus.Active, cancellationToken);
    }

    public async Task<int> GetActiveEnrollmentCountByTeacherAsync(Guid teacherId, CancellationToken cancellationToken = default)
    {
        return await _context.Enrollments
            .Where(e => e.Status == EnrollmentStatus.Active)
            .Where(e => _context.Classrooms.Any(c => c.Id == e.ClassroomId && c.TeacherId == teacherId))
            .CountAsync(cancellationToken);
    }

    public async Task AddAsync(Enrollment enrollment, CancellationToken cancellationToken = default)
    {
        await _context.Enrollments.AddAsync(enrollment, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
