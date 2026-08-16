namespace Draya.Domain.Identity;

public interface ITeacherRepository
{
    Task<Teacher?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<List<Teacher>> GetByUserIdsAsync(IEnumerable<Guid> userIds, CancellationToken cancellationToken = default);
    Task<IEnumerable<Teacher>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Teacher teacher, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

