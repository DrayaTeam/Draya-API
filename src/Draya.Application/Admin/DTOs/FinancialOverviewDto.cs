namespace Draya.Application.Admin.DTOs;

public record FinancialOverviewDto(
    decimal TotalClassroomRevenue,
    decimal TotalCommissionCollected,
    decimal TotalTeacherEarnedBalance,
    decimal TotalTeacherPurchasedBalance,
    decimal TotalOutstandingEarnedBalance,
    decimal TotalTopUps,
    decimal TotalAIExamCharges
);
