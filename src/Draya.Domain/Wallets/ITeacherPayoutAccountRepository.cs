namespace Draya.Domain.Wallets;

public interface ITeacherPayoutAccountRepository
{
    Task AddAsync(TeacherPayoutAccount account, CancellationToken cancellationToken = default);
    Task<TeacherPayoutAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<TeacherPayoutAccount>> GetByTeacherIdAsync(Guid teacherId, CancellationToken cancellationToken = default);
    Task<TeacherPayoutAccount?> GetDefaultByTeacherIdAsync(Guid teacherId, CancellationToken cancellationToken = default);
    Task UpdateAsync(TeacherPayoutAccount account, CancellationToken cancellationToken = default);
    Task DeleteAsync(TeacherPayoutAccount account, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
