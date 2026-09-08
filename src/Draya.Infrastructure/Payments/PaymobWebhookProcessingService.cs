using Draya.Application.Payments.Services;
using Draya.Domain.Admin;
using Draya.Domain.Classrooms;
using Draya.Domain.Payments;
using Draya.Domain.Wallets;
using Draya.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Draya.Infrastructure.Payments;

public class PaymobWebhookProcessingService : IPaymobWebhookProcessingService
{
    private readonly ApplicationDbContext _context;

    public PaymobWebhookProcessingService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> ProcessWebhookAsync(Guid paymentTransactionId, bool isSuccess, string rawPayload, CancellationToken cancellationToken = default)
    {
        var payment = await _context.PaymentTransactions
            .FirstOrDefaultAsync(p => p.Id == paymentTransactionId, cancellationToken);

        if (payment == null)
        {
            throw new KeyNotFoundException($"Payment transaction '{paymentTransactionId}' not found.");
        }

        if (payment.Status == PaymentStatus.Completed || payment.Status == PaymentStatus.Failed)
        {
            return true;
        }

        if (!isSuccess)
        {
            payment.Status = PaymentStatus.Failed;
            payment.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        if (payment.Purpose == PaymentPurpose.ClassroomEnrollment)
        {
            if (!payment.ClassroomId.HasValue)
            {
                throw new InvalidOperationException("Classroom ID is required for classroom enrollment payments.");
            }

            var classroom = await _context.Classrooms
                .FirstOrDefaultAsync(c => c.Id == payment.ClassroomId.Value, cancellationToken);

            if (classroom == null)
            {
                throw new InvalidOperationException($"Classroom '{payment.ClassroomId}' not found.");
            }

            var settings = await _context.PlatformSettings.FirstOrDefaultAsync(cancellationToken)
                ?? new PlatformSetting { PlatformCommissionPercent = 5.00m };

            var commissionPercent = settings.PlatformCommissionPercent;
            var commissionAmount = Math.Round(payment.GrossAmount * (commissionPercent / 100m), 2);
            var teacherAmount = payment.GrossAmount - commissionAmount;

            payment.CommissionPercent = commissionPercent;
            payment.CommissionAmount = commissionAmount;
            payment.TeacherAmount = teacherAmount;
            payment.Status = PaymentStatus.Completed;
            payment.UpdatedAt = DateTime.UtcNow;

            var wallet = await _context.TeacherWallets
                .FirstOrDefaultAsync(w => w.TeacherId == classroom.TeacherId, cancellationToken);

            if (wallet == null)
            {
                wallet = new TeacherWallet
                {
                    Id = Guid.NewGuid(),
                    TeacherId = classroom.TeacherId,
                    EarnedBalance = 0m,
                    PurchasedBalance = 0m,
                    CreatedAt = DateTime.UtcNow
                };
                await _context.TeacherWallets.AddAsync(wallet, cancellationToken);
            }

            wallet.EarnedBalance += teacherAmount;
            wallet.UpdatedAt = DateTime.UtcNow;

            var ledgerTx = new WalletTransaction
            {
                Id = Guid.NewGuid(),
                TeacherId = classroom.TeacherId,
                Type = WalletTransactionType.ClassroomEarning,
                Amount = teacherAmount,
                BalanceType = WalletBalanceType.Earned,
                ReferenceId = payment.Id,
                Description = $"Classroom enrollment earning for classroom '{classroom.Name}'",
                CreatedAt = DateTime.UtcNow
            };
            await _context.WalletTransactions.AddAsync(ledgerTx, cancellationToken);

            var existingEnrollment = await _context.Enrollments
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(e => e.StudentId == payment.PayerId && e.ClassroomId == classroom.Id, cancellationToken);

            if (existingEnrollment == null)
            {
                var enrollment = new Enrollment
                {
                    StudentId = payment.PayerId,
                    ClassroomId = classroom.Id,
                    Status = EnrollmentStatus.Active
                };
                await _context.Enrollments.AddAsync(enrollment, cancellationToken);
            }
            else
            {
                existingEnrollment.Status = EnrollmentStatus.Active;
                existingEnrollment.EnrolledAt = DateTime.UtcNow;
            }
        }
        else if (payment.Purpose == PaymentPurpose.TeacherTopUp)
        {
            payment.Status = PaymentStatus.Completed;
            payment.UpdatedAt = DateTime.UtcNow;

            var wallet = await _context.TeacherWallets
                .FirstOrDefaultAsync(w => w.TeacherId == payment.PayerId, cancellationToken);

            if (wallet == null)
            {
                wallet = new TeacherWallet
                {
                    Id = Guid.NewGuid(),
                    TeacherId = payment.PayerId,
                    EarnedBalance = 0m,
                    PurchasedBalance = 0m,
                    CreatedAt = DateTime.UtcNow
                };
                await _context.TeacherWallets.AddAsync(wallet, cancellationToken);
            }

            wallet.PurchasedBalance += payment.GrossAmount;
            wallet.UpdatedAt = DateTime.UtcNow;

            var ledgerTx = new WalletTransaction
            {
                Id = Guid.NewGuid(),
                TeacherId = payment.PayerId,
                Type = WalletTransactionType.TeacherTopUp,
                Amount = payment.GrossAmount,
                BalanceType = WalletBalanceType.Purchased,
                ReferenceId = payment.Id,
                Description = $"Teacher top-up payment ({payment.GrossAmount} EGP)",
                CreatedAt = DateTime.UtcNow
            };
            await _context.WalletTransactions.AddAsync(ledgerTx, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
