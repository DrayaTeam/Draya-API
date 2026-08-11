namespace Draya.Domain.Admin;

public record FinancialOverviewData(
    decimal TotalClassroomRevenue,
    decimal TotalCommissionCollected,
    decimal TotalTeacherEarnedBalance,
    decimal TotalTeacherPurchasedBalance,
    decimal TotalOutstandingEarnedBalance,
    decimal TotalTopUps,
    decimal TotalAIExamCharges
);

public interface IFinancialOverviewRepository
{
    Task<FinancialOverviewData> GetFinancialOverviewAsync(CancellationToken cancellationToken = default);
}
