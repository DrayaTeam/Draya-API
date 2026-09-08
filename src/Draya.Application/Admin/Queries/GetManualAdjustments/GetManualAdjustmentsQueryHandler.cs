using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Draya.Application.Admin.DTOs;
using Draya.Application.Common.Models;
using Draya.Domain.Wallets;
using MediatR;

namespace Draya.Application.Admin.Queries.GetManualAdjustments;

public class GetManualAdjustmentsQueryHandler : IRequestHandler<GetManualAdjustmentsQuery, PaginatedResult<ManualAdjustmentDto>>
{
    private readonly IWalletTransactionRepository _transactionRepository;

    public GetManualAdjustmentsQueryHandler(IWalletTransactionRepository transactionRepository)
    {
        _transactionRepository = transactionRepository;
    }

    public async Task<PaginatedResult<ManualAdjustmentDto>> Handle(GetManualAdjustmentsQuery request, CancellationToken cancellationToken)
    {
        var query = _transactionRepository.GetQueryable()
            .Where(t => t.Type == WalletTransactionType.Adjustment);

        if (request.TeacherId.HasValue)
        {
            query = query.Where(t => t.TeacherId == request.TeacherId.Value);
        }

        var totalCount = query.Count();
        
        var transactions = query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        var items = transactions.Select(t => new ManualAdjustmentDto(
            t.Id,
            t.TeacherId,
            t.Amount,
            t.BalanceType,
            t.Description,
            t.CreatedAt
        )).ToList();

        return new PaginatedResult<ManualAdjustmentDto>(
            items,
            totalCount,
            request.PageNumber,
            request.PageSize
        );
    }
}
