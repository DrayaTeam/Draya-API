using Draya.Domain.Wallets;
using MediatR;

namespace Draya.Application.Admin.Commands.MarkWithdrawalAsPaid;

public record MarkWithdrawalAsPaidCommand(
    Guid RequestId, 
    string? AdminNote, 
    Guid AdminId
) : IRequest<bool>;

public class MarkWithdrawalAsPaidCommandHandler : IRequestHandler<MarkWithdrawalAsPaidCommand, bool>
{
    private readonly IWithdrawalRequestRepository _withdrawalRepository;
    private readonly ITeacherWalletRepository _walletRepository;
    private readonly IWalletTransactionRepository _transactionRepository;

    public MarkWithdrawalAsPaidCommandHandler(
        IWithdrawalRequestRepository withdrawalRepository,
        ITeacherWalletRepository walletRepository,
        IWalletTransactionRepository transactionRepository)
    {
        _withdrawalRepository = withdrawalRepository;
        _walletRepository = walletRepository;
        _transactionRepository = transactionRepository;
    }

    public async Task<bool> Handle(MarkWithdrawalAsPaidCommand request, CancellationToken cancellationToken)
    {
        var withdrawal = await _withdrawalRepository.GetByIdAsync(request.RequestId, cancellationToken);
        if (withdrawal == null)
        {
            throw new KeyNotFoundException("Withdrawal request not found.");
        }

        if (withdrawal.Status != WithdrawalStatus.Approved)
        {
            throw new InvalidOperationException("Withdrawal request must be Approved before being marked as Paid.");
        }

        var wallet = await _walletRepository.GetByTeacherIdAsync(withdrawal.TeacherId, cancellationToken);
        if (wallet == null)
        {
            throw new InvalidOperationException("Teacher wallet not found.");
        }

        wallet.EarnedBalance = Math.Max(0m, wallet.EarnedBalance - withdrawal.Amount);
        wallet.UpdatedAt = DateTime.UtcNow;
        await _walletRepository.UpdateAsync(wallet, cancellationToken);

        var ledgerTx = new WalletTransaction
        {
            Id = Guid.NewGuid(),
            TeacherId = withdrawal.TeacherId,
            Type = WalletTransactionType.Withdrawal,
            Amount = -withdrawal.Amount,
            BalanceType = WalletBalanceType.Earned,
            ReferenceId = withdrawal.Id,
            Description = request.AdminNote ?? $"Withdrawal of {withdrawal.Amount} EGP processed.",
            CreatedAt = DateTime.UtcNow
        };
        await _transactionRepository.AddAsync(ledgerTx, cancellationToken);

        withdrawal.Status = WithdrawalStatus.Paid;
        withdrawal.AdminNote = request.AdminNote;
        withdrawal.ProcessedAt = DateTime.UtcNow;
        await _withdrawalRepository.UpdateAsync(withdrawal, cancellationToken);

        await _withdrawalRepository.SaveChangesAsync(cancellationToken);
        return true;
    }
}
