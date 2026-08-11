using Draya.Application.Wallets.DTOs;
using Draya.Domain.Wallets;
using MediatR;

namespace Draya.Application.Wallets.Queries.GetWalletBalance;

public record GetWalletBalanceQuery(Guid TeacherId) : IRequest<WalletBalanceDto>;

public class GetWalletBalanceQueryHandler : IRequestHandler<GetWalletBalanceQuery, WalletBalanceDto>
{
    private readonly ITeacherWalletRepository _walletRepository;
    private readonly IWithdrawalRequestRepository _withdrawalRepository;

    public GetWalletBalanceQueryHandler(
        ITeacherWalletRepository walletRepository,
        IWithdrawalRequestRepository withdrawalRepository)
    {
        _walletRepository = walletRepository;
        _withdrawalRepository = withdrawalRepository;
    }

    public async Task<WalletBalanceDto> Handle(GetWalletBalanceQuery request, CancellationToken cancellationToken)
    {
        var wallet = await _walletRepository.GetByTeacherIdAsync(request.TeacherId, cancellationToken);
        if (wallet == null)
        {
            return new WalletBalanceDto(0m, 0m, 0m);
        }

        var pendingWithdrawals = await _withdrawalRepository.GetPendingTotalAmountByTeacherIdAsync(request.TeacherId, cancellationToken);
        var availableEarned = Math.Max(0m, wallet.EarnedBalance - pendingWithdrawals);

        return new WalletBalanceDto(
            wallet.EarnedBalance,
            wallet.PurchasedBalance,
            availableEarned
        );
    }
}
