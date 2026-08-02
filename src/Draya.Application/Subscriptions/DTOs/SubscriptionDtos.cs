namespace Draya.Application.Subscriptions.DTOs;
public record SubscriptionPlanSummaryDto(string Name, int MaxStudents, int MaxStorageMB, int MonthlyExamQuota, decimal PriceMonthly);
public record SubscriptionUsageDto(int CurrentStudentsCount, int MaxStudents, decimal StorageUsedMB, int MaxStorageMB, int ExamsGeneratedCount, int MonthlyExamQuota);
