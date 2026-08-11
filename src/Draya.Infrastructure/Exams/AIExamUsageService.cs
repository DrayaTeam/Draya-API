using Draya.Application.Exams.Services;
using Draya.Domain.Admin;
using Draya.Domain.Subscriptions;
using Draya.Domain.Wallets;
using Draya.Domain.Wallets.Exceptions;
using Draya.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Draya.Infrastructure.Exams;

public class AIExamUsageService : IAIExamUsageService
{
    private readonly ApplicationDbContext _context;

    public AIExamUsageService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> ValidateExamGenerationQuotaAsync(Guid teacherId, CancellationToken cancellationToken = default)
    {
        var currentMonth = new DateOnly(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        var usageCounter = await _context.UsageCounters
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.TeacherId == teacherId && u.PeriodMonth == currentMonth, cancellationToken);

        var settings = await _context.PlatformSettings.AsNoTracking().FirstOrDefaultAsync(cancellationToken)
            ?? new PlatformSetting { FreeMonthlyAIExamQuota = 3, AIExamPrice = 20.00m };

        var freeUsed = usageCounter?.FreeExamsUsed ?? 0;
        if (freeUsed < settings.FreeMonthlyAIExamQuota)
        {
            return true;
        }

        var wallet = await _context.TeacherWallets
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.TeacherId == teacherId, cancellationToken);

        var combinedBalance = (wallet?.EarnedBalance ?? 0m) + (wallet?.PurchasedBalance ?? 0m);
        if (combinedBalance < settings.AIExamPrice)
        {
            throw new InsufficientBalanceException($"Monthly free quota ({settings.FreeMonthlyAIExamQuota} exams) reached. AI Exam price is {settings.AIExamPrice} EGP, but available wallet balance is {combinedBalance} EGP.");
        }

        return true;
    }

    public async Task RecordSuccessfulExamGenerationAsync(Guid teacherId, Guid? examId = null, CancellationToken cancellationToken = default)
    {
        var currentMonth = new DateOnly(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        var usageCounter = await _context.UsageCounters
            .FirstOrDefaultAsync(u => u.TeacherId == teacherId && u.PeriodMonth == currentMonth, cancellationToken);

        if (usageCounter == null)
        {
            usageCounter = new UsageCounter
            {
                Id = Guid.NewGuid(),
                TeacherId = teacherId,
                PeriodMonth = currentMonth,
                FreeExamsUsed = 0,
                PaidExamsGenerated = 0,
                CreatedAt = DateTime.UtcNow
            };
            await _context.UsageCounters.AddAsync(usageCounter, cancellationToken);
        }

        var settings = await _context.PlatformSettings.FirstOrDefaultAsync(cancellationToken)
            ?? new PlatformSetting { FreeMonthlyAIExamQuota = 3, AIExamPrice = 20.00m };

        if (usageCounter.FreeExamsUsed < settings.FreeMonthlyAIExamQuota)
        {
            usageCounter.FreeExamsUsed += 1;
            usageCounter.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            var wallet = await _context.TeacherWallets
                .FirstOrDefaultAsync(w => w.TeacherId == teacherId, cancellationToken);

            if (wallet == null)
            {
                throw new InsufficientBalanceException("Teacher wallet not found for paid exam generation.");
            }

            var price = settings.AIExamPrice;
            var combined = wallet.EarnedBalance + wallet.PurchasedBalance;
            if (combined < price)
            {
                throw new InsufficientBalanceException("Insufficient wallet balance for AI exam generation.");
            }

            var earnedDeduction = Math.Min(wallet.EarnedBalance, price);
            var purchasedDeduction = price - earnedDeduction;

            wallet.EarnedBalance -= earnedDeduction;
            wallet.PurchasedBalance -= purchasedDeduction;
            wallet.UpdatedAt = DateTime.UtcNow;

            var balanceType = earnedDeduction > 0 ? WalletBalanceType.Earned : WalletBalanceType.Purchased;
            var ledgerTx = new WalletTransaction
            {
                Id = Guid.NewGuid(),
                TeacherId = teacherId,
                Type = WalletTransactionType.AIExamCharge,
                Amount = -price,
                BalanceType = balanceType,
                ReferenceId = examId,
                Description = $"AI Exam generation fee ({price} EGP)",
                CreatedAt = DateTime.UtcNow
            };
            await _context.WalletTransactions.AddAsync(ledgerTx, cancellationToken);

            usageCounter.PaidExamsGenerated += 1;
            usageCounter.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
