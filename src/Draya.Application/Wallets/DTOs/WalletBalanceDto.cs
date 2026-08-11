namespace Draya.Application.Wallets.DTOs;

public record WalletBalanceDto(
    decimal EarnedBalance,
    decimal PurchasedBalance,
    decimal AvailableEarnedBalance
);
