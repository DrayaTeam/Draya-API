namespace Draya.Application.Exams.DTOs;

public record AIExamQuotaDto(
    int FreeMonthlyQuota,
    int FreeExamsUsed,
    int RemainingFreeQuota,
    decimal AIExamPrice,
    bool HasSufficientBalanceForPaid
);
