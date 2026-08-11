using Draya.Domain.Wallets;

namespace Draya.Application.Wallets.DTOs;

public record TeacherPayoutAccountDto(
    Guid Id,
    Guid TeacherId,
    PayoutAccountType AccountType,
    string AccountName,
    string AccountIdentifier,
    bool IsDefault,
    DateTime CreatedAt
);
