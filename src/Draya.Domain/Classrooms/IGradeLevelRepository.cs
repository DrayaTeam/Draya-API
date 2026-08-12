namespace Draya.Domain.Classrooms;

public interface IGradeLevelRepository
{
    Task<GradeLevel?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<GradeLevel>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(GradeLevel entity, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
