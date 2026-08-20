namespace Draya.Domain.Wallets;

public interface IWalletTransactionRepository
{
    Task AddAsync(WalletTransaction transaction, CancellationToken cancellationToken = default);
    Task<(List<WalletTransaction> Items, int TotalCount)> GetPaginatedByTeacherIdAsync(
        Guid teacherId, 
        int pageNumber, 
        int pageSize, 
        CancellationToken cancellationToken = default);
    System.Linq.IQueryable<WalletTransaction> GetQueryable();
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
