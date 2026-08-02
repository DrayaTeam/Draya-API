namespace Draya.Domain.Classrooms;

public interface IEnrollmentRepository
{
    Task<Enrollment?> GetByStudentAndClassroomAsync(Guid studentId, Guid classroomId, CancellationToken cancellationToken = default);
    Task<List<Enrollment>> GetByClassroomIdAsync(Guid classroomId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<int> GetCountByClassroomIdAsync(Guid classroomId, CancellationToken cancellationToken = default);
    Task<int> GetActiveEnrollmentCountByTeacherAsync(Guid teacherId, CancellationToken cancellationToken = default);
    Task AddAsync(Enrollment enrollment, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
