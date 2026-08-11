using Draya.Domain.Wallets;
using MediatR;

namespace Draya.Application.Admin.Commands.AdjustTeacherBalance;

public record AdjustTeacherBalanceCommand(
    Guid TeacherId,
    decimal Amount,
    WalletBalanceType BalanceType,
    string Reason,
    Guid AdminId
) : IRequest<bool>;

public class AdjustTeacherBalanceCommandHandler : IRequestHandler<AdjustTeacherBalanceCommand, bool>
{
    private readonly ITeacherWalletRepository _walletRepository;
    private readonly IWalletTransactionRepository _transactionRepository;

    public AdjustTeacherBalanceCommandHandler(
        ITeacherWalletRepository walletRepository,
        IWalletTransactionRepository transactionRepository)
    {
        _walletRepository = walletRepository;
        _transactionRepository = transactionRepository;
    }

    public async Task<bool> Handle(AdjustTeacherBalanceCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new ArgumentException("Reason is required for manual balance adjustments.", nameof(request.Reason));
        }

        if (request.Amount == 0)
        {
            throw new ArgumentException("Adjustment amount cannot be zero.", nameof(request.Amount));
        }

        var wallet = await _walletRepository.GetByTeacherIdAsync(request.TeacherId, cancellationToken);
        if (wallet == null)
        {
            wallet = new TeacherWallet
            {
                Id = Guid.NewGuid(),
                TeacherId = request.TeacherId,
                EarnedBalance = 0m,
                PurchasedBalance = 0m,
                CreatedAt = DateTime.UtcNow
            };
            await _walletRepository.AddAsync(wallet, cancellationToken);
        }

        if (request.BalanceType == WalletBalanceType.Earned)
        {
            wallet.EarnedBalance += request.Amount;
        }
        else
        {
            wallet.PurchasedBalance += request.Amount;
        }

        wallet.UpdatedAt = DateTime.UtcNow;
        await _walletRepository.UpdateAsync(wallet, cancellationToken);

        var ledgerTx = new WalletTransaction
        {
            Id = Guid.NewGuid(),
            TeacherId = request.TeacherId,
            Type = WalletTransactionType.Adjustment,
            Amount = request.Amount,
            BalanceType = request.BalanceType,
            ReferenceId = null,
            Description = $"Admin adjustment ({request.Reason})",
            CreatedAt = DateTime.UtcNow
        };

        await _transactionRepository.AddAsync(ledgerTx, cancellationToken);
        await _walletRepository.SaveChangesAsync(cancellationToken);
        return true;
    }
}
