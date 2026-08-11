using Draya.Application.Wallets.DTOs;
using Draya.Domain.Wallets;

namespace Draya.Application.Admin.DTOs;

public record AdminWithdrawalRequestDto(
    Guid Id,
    Guid TeacherId,
    string TeacherFullName,
    string TeacherEmail,
    decimal Amount,
    WithdrawalStatus Status,
    DateTime RequestedAt,
    DateTime? ProcessedAt,
    string? AdminNote,
    string? RejectionReason,
    List<TeacherPayoutAccountDto> PayoutAccounts
);
