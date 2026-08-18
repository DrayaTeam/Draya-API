namespace Draya.Domain.Classrooms;

public interface IClassroomFeedbackRepository
{
    Task<bool> ExistsAsync(Guid classroomId, Guid studentId, CancellationToken cancellationToken = default);
    Task AddAsync(ClassroomFeedback feedback, CancellationToken cancellationToken = default);
    Task<(List<ClassroomFeedback> Items, int TotalCount, double AverageRating)> GetByClassroomIdAsync(
        Guid classroomId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
