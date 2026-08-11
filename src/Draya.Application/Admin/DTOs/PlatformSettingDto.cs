namespace Draya.Application.Admin.DTOs;

public record PlatformSettingDto(
    Guid Id,
    decimal AIExamPrice,
    int FreeMonthlyAIExamQuota,
    decimal PlatformCommissionPercent,
    DateTime UpdatedAt,
    Guid? UpdatedByAdminId
);
