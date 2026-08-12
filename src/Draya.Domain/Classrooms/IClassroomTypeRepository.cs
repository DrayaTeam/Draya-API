namespace Draya.Domain.Classrooms;

public interface IClassroomTypeRepository
{
    Task<ClassroomType?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<ClassroomType>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(ClassroomType entity, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
