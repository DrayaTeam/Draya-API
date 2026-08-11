namespace Draya.Domain.Wallets;

public interface ITeacherWalletRepository
{
    Task<TeacherWallet?> GetByTeacherIdAsync(Guid teacherId, CancellationToken cancellationToken = default);
    Task AddAsync(TeacherWallet wallet, CancellationToken cancellationToken = default);
    Task UpdateAsync(TeacherWallet wallet, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
