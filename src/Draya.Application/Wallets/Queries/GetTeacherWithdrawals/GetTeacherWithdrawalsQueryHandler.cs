using Draya.Application.Common.Models;
using Draya.Application.Wallets.DTOs;
using Draya.Domain.Wallets;
using MediatR;

namespace Draya.Application.Wallets.Queries.GetTeacherWithdrawals;

public record GetTeacherWithdrawalsQuery(
    Guid TeacherId, 
    int PageNumber = 1, 
    int PageSize = 20
) : IRequest<PaginatedResult<WithdrawalRequestDto>>;

public class GetTeacherWithdrawalsQueryHandler : IRequestHandler<GetTeacherWithdrawalsQuery, PaginatedResult<WithdrawalRequestDto>>
{
    private readonly IWithdrawalRequestRepository _withdrawalRepository;

    public GetTeacherWithdrawalsQueryHandler(IWithdrawalRequestRepository withdrawalRepository)
    {
        _withdrawalRepository = withdrawalRepository;
    }

    public async Task<PaginatedResult<WithdrawalRequestDto>> Handle(GetTeacherWithdrawalsQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.PageNumber);
        var size = Math.Clamp(request.PageSize, 1, 100);

        var (items, count) = await _withdrawalRepository.GetPaginatedByTeacherIdAsync(
            request.TeacherId, 
            page, 
            size, 
            cancellationToken);

        var dtos = items.Select(w => new WithdrawalRequestDto(
            w.Id,
            w.TeacherId,
            w.Amount,
            w.Status,
            w.RequestedAt,
            w.ProcessedAt,
            w.AdminNote,
            w.RejectionReason
        )).ToList();

        return new PaginatedResult<WithdrawalRequestDto>(dtos, count, page, size);
    }
}
