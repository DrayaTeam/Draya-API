namespace Draya.Application.Identity.DTOs;

public record TeacherSearchDto(
    Guid Id,
    string Name,
    string Email,
    decimal EarnedBalance,
    decimal PurchasedBalance
);
