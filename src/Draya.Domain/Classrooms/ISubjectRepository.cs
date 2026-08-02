namespace Draya.Domain.Classrooms;

public interface ISubjectRepository
{
    Task<List<Subject>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Subject?> GetByIdAsync(Guid subjectId, CancellationToken cancellationToken = default);
    Task<Subject?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task AddAsync(Subject subject, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
