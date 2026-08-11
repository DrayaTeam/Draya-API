using Draya.Application.Wallets.DTOs;
using Draya.Domain.Wallets;
using MediatR;

namespace Draya.Application.Wallets.Queries.GetTeacherPayoutAccounts;

public record GetTeacherPayoutAccountsQuery(Guid TeacherId) : IRequest<List<TeacherPayoutAccountDto>>;

public class GetTeacherPayoutAccountsQueryHandler : IRequestHandler<GetTeacherPayoutAccountsQuery, List<TeacherPayoutAccountDto>>
{
    private readonly ITeacherPayoutAccountRepository _payoutAccountRepository;

    public GetTeacherPayoutAccountsQueryHandler(ITeacherPayoutAccountRepository payoutAccountRepository)
    {
        _payoutAccountRepository = payoutAccountRepository;
    }

    public async Task<List<TeacherPayoutAccountDto>> Handle(GetTeacherPayoutAccountsQuery request, CancellationToken cancellationToken)
    {
        var accounts = await _payoutAccountRepository.GetByTeacherIdAsync(request.TeacherId, cancellationToken);
        return accounts.Select(a => new TeacherPayoutAccountDto(
            a.Id,
            a.TeacherId,
            a.AccountType,
            a.AccountName,
            a.AccountIdentifier,
            a.IsDefault,
            a.CreatedAt
        )).ToList();
    }
}
