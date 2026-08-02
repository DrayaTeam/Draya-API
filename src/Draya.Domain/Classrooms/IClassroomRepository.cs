namespace Draya.Domain.Classrooms;

public interface IClassroomRepository
{
    Task<Classroom?> GetByIdAsync(Guid classroomId, CancellationToken cancellationToken = default);
    Task<Classroom?> GetByEnrollmentCodeAsync(string enrollmentCode, CancellationToken cancellationToken = default);
    Task<List<Classroom>> GetByTeacherIdAsync(Guid teacherId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<int> GetCountByTeacherIdAsync(Guid teacherId, CancellationToken cancellationToken = default);
    Task<List<Classroom>> GetByStudentIdAsync(Guid studentId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<int> GetCountByStudentIdAsync(Guid studentId, CancellationToken cancellationToken = default);
    Task<bool> IsStudentEnrolledAsync(Guid studentId, Guid classroomId, CancellationToken cancellationToken = default);
    Task AddAsync(Classroom classroom, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
