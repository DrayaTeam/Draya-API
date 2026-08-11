using Draya.Domain.Wallets;

namespace Draya.Application.Wallets.DTOs;

public record WalletTransactionDto(
    Guid Id,
    WalletTransactionType Type,
    decimal Amount,
    WalletBalanceType BalanceType,
    Guid? ReferenceId,
    string? Description,
    DateTime CreatedAt
);
