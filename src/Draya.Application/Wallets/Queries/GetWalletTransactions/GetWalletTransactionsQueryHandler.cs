using Draya.Application.Common.Models;
using Draya.Application.Wallets.DTOs;
using Draya.Domain.Wallets;
using MediatR;

namespace Draya.Application.Wallets.Queries.GetWalletTransactions;

public record GetWalletTransactionsQuery(
    Guid TeacherId, 
    int PageNumber = 1, 
    int PageSize = 20
) : IRequest<PaginatedResult<WalletTransactionDto>>;

public class GetWalletTransactionsQueryHandler : IRequestHandler<GetWalletTransactionsQuery, PaginatedResult<WalletTransactionDto>>
{
    private readonly IWalletTransactionRepository _transactionRepository;

    public GetWalletTransactionsQueryHandler(IWalletTransactionRepository transactionRepository)
    {
        _transactionRepository = transactionRepository;
    }

    public async Task<PaginatedResult<WalletTransactionDto>> Handle(GetWalletTransactionsQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.PageNumber);
        var size = Math.Clamp(request.PageSize, 1, 100);

        var (items, count) = await _transactionRepository.GetPaginatedByTeacherIdAsync(
            request.TeacherId, 
            page, 
            size, 
            cancellationToken);

        var dtos = items.Select(t => new WalletTransactionDto(
            t.Id,
            t.Type,
            t.Amount,
            t.BalanceType,
            t.ReferenceId,
            t.Description,
            t.CreatedAt
        )).ToList();

        return new PaginatedResult<WalletTransactionDto>(dtos, count, page, size);
    }
}
