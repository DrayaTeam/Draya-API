using Draya.Application.Wallets.DTOs;
using Draya.Domain.Wallets;
using Draya.Domain.Wallets.Exceptions;
using MediatR;

namespace Draya.Application.Wallets.Commands.RequestWithdrawal;

public record RequestWithdrawalCommand(
    Guid TeacherId, 
    decimal Amount
) : IRequest<WithdrawalRequestDto>;

public class RequestWithdrawalCommandHandler : IRequestHandler<RequestWithdrawalCommand, WithdrawalRequestDto>
{
    private readonly ITeacherWalletRepository _walletRepository;
    private readonly IWithdrawalRequestRepository _withdrawalRepository;

    public RequestWithdrawalCommandHandler(
        ITeacherWalletRepository walletRepository,
        IWithdrawalRequestRepository withdrawalRepository)
    {
        _walletRepository = walletRepository;
        _withdrawalRepository = withdrawalRepository;
    }

    public async Task<WithdrawalRequestDto> Handle(RequestWithdrawalCommand request, CancellationToken cancellationToken)
    {
        if (request.Amount <= 0)
        {
            throw new ArgumentException("Withdrawal amount must be greater than zero.", nameof(request.Amount));
        }

        var wallet = await _walletRepository.GetByTeacherIdAsync(request.TeacherId, cancellationToken);
        if (wallet == null)
        {
            throw new InsufficientBalanceException("Teacher wallet not found.");
        }

        var pendingAmount = await _withdrawalRepository.GetPendingTotalAmountByTeacherIdAsync(request.TeacherId, cancellationToken);
        var availableEarned = wallet.EarnedBalance - pendingAmount;

        if (availableEarned < request.Amount)
        {
            throw new InsufficientBalanceException($"Requested amount ({request.Amount} EGP) exceeds available withdrawable balance ({availableEarned} EGP).");
        }

        var withdrawalRequest = new WithdrawalRequest
        {
            Id = Guid.NewGuid(),
            TeacherId = request.TeacherId,
            Amount = request.Amount,
            Status = WithdrawalStatus.Pending,
            RequestedAt = DateTime.UtcNow
        };

        await _withdrawalRepository.AddAsync(withdrawalRequest, cancellationToken);
        await _withdrawalRepository.SaveChangesAsync(cancellationToken);

        return new WithdrawalRequestDto(
            withdrawalRequest.Id,
            withdrawalRequest.TeacherId,
            withdrawalRequest.Amount,
            withdrawalRequest.Status,
            withdrawalRequest.RequestedAt,
            withdrawalRequest.ProcessedAt,
            withdrawalRequest.AdminNote,
            withdrawalRequest.RejectionReason
        );
    }
}
