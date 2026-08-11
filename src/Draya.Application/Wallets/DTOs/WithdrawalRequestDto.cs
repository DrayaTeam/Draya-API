using Draya.Domain.Wallets;

namespace Draya.Application.Wallets.DTOs;

public record WithdrawalRequestDto(
    Guid Id,
    Guid TeacherId,
    decimal Amount,
    WithdrawalStatus Status,
    DateTime RequestedAt,
    DateTime? ProcessedAt,
    string? AdminNote,
    string? RejectionReason
);
