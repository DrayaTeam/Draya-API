namespace Draya.Domain.Classrooms;

public interface ISectionRepository
{
    Task<ClassroomSection?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<ClassroomSection>> GetByClassroomIdAsync(Guid classroomId, CancellationToken cancellationToken = default);
    Task AddAsync(ClassroomSection section, CancellationToken cancellationToken = default);
    void Update(ClassroomSection section);
    void Remove(ClassroomSection section);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
