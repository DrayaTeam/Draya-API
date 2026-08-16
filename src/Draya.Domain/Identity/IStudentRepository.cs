namespace Draya.Domain.Identity;

public interface IStudentRepository
{
    Task<Student?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<List<Student>> GetByUserIdsAsync(IEnumerable<Guid> userIds, CancellationToken cancellationToken = default);
    Task AddAsync(Student student, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

