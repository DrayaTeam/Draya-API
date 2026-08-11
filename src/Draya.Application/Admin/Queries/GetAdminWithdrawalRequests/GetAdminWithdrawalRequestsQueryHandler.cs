using Draya.Application.Admin.DTOs;
using Draya.Application.Common.Models;
using Draya.Application.Wallets.DTOs;
using Draya.Domain.Identity;
using Draya.Domain.Wallets;
using MediatR;

namespace Draya.Application.Admin.Queries.GetAdminWithdrawalRequests;

public record GetAdminWithdrawalRequestsQuery(
    WithdrawalStatus? StatusFilter,
    int PageNumber = 1,
    int PageSize = 20
) : IRequest<PaginatedResult<AdminWithdrawalRequestDto>>;

public class GetAdminWithdrawalRequestsQueryHandler : IRequestHandler<GetAdminWithdrawalRequestsQuery, PaginatedResult<AdminWithdrawalRequestDto>>
{
    private readonly IWithdrawalRequestRepository _withdrawalRepository;
    private readonly ITeacherRepository _teacherRepository;
    private readonly ITeacherPayoutAccountRepository _payoutAccountRepository;

    public GetAdminWithdrawalRequestsQueryHandler(
        IWithdrawalRequestRepository withdrawalRepository,
        ITeacherRepository teacherRepository,
        ITeacherPayoutAccountRepository payoutAccountRepository)
    {
        _withdrawalRepository = withdrawalRepository;
        _teacherRepository = teacherRepository;
        _payoutAccountRepository = payoutAccountRepository;
    }

    public async Task<PaginatedResult<AdminWithdrawalRequestDto>> Handle(GetAdminWithdrawalRequestsQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.PageNumber);
        var size = Math.Clamp(request.PageSize, 1, 100);

        var (items, totalCount) = await _withdrawalRepository.GetPaginatedAllAsync(
            request.StatusFilter,
            page,
            size,
            cancellationToken);

        var dtos = new List<AdminWithdrawalRequestDto>();

        foreach (var w in items)
        {
            var teacher = await _teacherRepository.GetByUserIdAsync(w.TeacherId, cancellationToken);
            var teacherName = teacher?.FullName ?? "Unknown Teacher";
            var teacherEmail = string.Empty; // Populated from Teacher profile/user context

            var accounts = await _payoutAccountRepository.GetByTeacherIdAsync(w.TeacherId, cancellationToken);
            var teacherAccounts = accounts.Select(a => new TeacherPayoutAccountDto(
                a.Id, a.TeacherId, a.AccountType, a.AccountName, a.AccountIdentifier, a.IsDefault, a.CreatedAt
            )).ToList();

            dtos.Add(new AdminWithdrawalRequestDto(
                w.Id,
                w.TeacherId,
                teacherName,
                teacherEmail,
                w.Amount,
                w.Status,
                w.RequestedAt,
                w.ProcessedAt,
                w.AdminNote,
                w.RejectionReason,
                teacherAccounts
            ));
        }

        return new PaginatedResult<AdminWithdrawalRequestDto>(dtos, totalCount, page, size);
    }
}
