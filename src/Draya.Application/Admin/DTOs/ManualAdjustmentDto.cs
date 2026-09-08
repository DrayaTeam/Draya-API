using System;
using Draya.Domain.Wallets;

namespace Draya.Application.Admin.DTOs;

public record ManualAdjustmentDto(
    Guid TransactionId,
    Guid TeacherId,
    decimal Amount,
    WalletBalanceType BalanceType,
    string? Description,
    DateTime CreatedAt
);
