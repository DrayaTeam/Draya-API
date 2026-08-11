using Draya.Domain.Admin;
using Draya.Domain.Payments;
using Draya.Domain.Wallets;
using Draya.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Draya.Infrastructure.Admin;

public class FinancialOverviewRepository : IFinancialOverviewRepository
{
    private readonly ApplicationDbContext _context;

    public FinancialOverviewRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<FinancialOverviewData> GetFinancialOverviewAsync(CancellationToken cancellationToken = default)
    {
        var classroomPayments = _context.PaymentTransactions
            .AsNoTracking()
            .Where(p => p.Purpose == PaymentPurpose.ClassroomEnrollment && p.Status == PaymentStatus.Completed);

        var totalClassroomRevenue = await classroomPayments.SumAsync(p => (decimal?)p.GrossAmount, cancellationToken) ?? 0m;
        var totalCommissionCollected = await classroomPayments.SumAsync(p => (decimal?)p.CommissionAmount, cancellationToken) ?? 0m;

        var wallets = _context.TeacherWallets.AsNoTracking();
        var totalTeacherEarnedBalance = await wallets.SumAsync(w => (decimal?)w.EarnedBalance, cancellationToken) ?? 0m;
        var totalTeacherPurchasedBalance = await wallets.SumAsync(w => (decimal?)w.PurchasedBalance, cancellationToken) ?? 0m;

        var totalTopUps = await _context.PaymentTransactions
            .AsNoTracking()
            .Where(p => p.Purpose == PaymentPurpose.TeacherTopUp && p.Status == PaymentStatus.Completed)
            .SumAsync(p => (decimal?)p.GrossAmount, cancellationToken) ?? 0m;

        var totalAIExamCharges = await _context.WalletTransactions
            .AsNoTracking()
            .Where(t => t.Type == WalletTransactionType.AIExamCharge)
            .SumAsync(t => (decimal?)Math.Abs(t.Amount), cancellationToken) ?? 0m;

        return new FinancialOverviewData(
            TotalClassroomRevenue: totalClassroomRevenue,
            TotalCommissionCollected: totalCommissionCollected,
            TotalTeacherEarnedBalance: totalTeacherEarnedBalance,
            TotalTeacherPurchasedBalance: totalTeacherPurchasedBalance,
            TotalOutstandingEarnedBalance: totalTeacherEarnedBalance,
            TotalTopUps: totalTopUps,
            TotalAIExamCharges: totalAIExamCharges
        );
    }
}
