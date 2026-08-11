namespace Draya.Domain.Wallets;

public interface IWithdrawalRequestRepository
{
    Task AddAsync(WithdrawalRequest request, CancellationToken cancellationToken = default);
    Task<WithdrawalRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<(List<WithdrawalRequest> Items, int TotalCount)> GetPaginatedByTeacherIdAsync(
        Guid teacherId, 
        int pageNumber, 
        int pageSize, 
        CancellationToken cancellationToken = default);
    Task<(List<WithdrawalRequest> Items, int TotalCount)> GetPaginatedAllAsync(
        WithdrawalStatus? statusFilter, 
        int pageNumber, 
        int pageSize, 
        CancellationToken cancellationToken = default);
    Task<decimal> GetPendingTotalAmountByTeacherIdAsync(Guid teacherId, CancellationToken cancellationToken = default);
    Task UpdateAsync(WithdrawalRequest request, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
