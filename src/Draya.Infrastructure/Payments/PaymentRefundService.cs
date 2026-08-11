using Draya.Application.Payments.Services;
using Draya.Domain.Payments;
using Draya.Domain.Wallets;
using Draya.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Draya.Infrastructure.Payments;

public class PaymentRefundService : IPaymentRefundService
{
    private readonly ApplicationDbContext _context;

    public PaymentRefundService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> RefundPaymentAsync(Guid paymentTransactionId, Guid adminId, CancellationToken cancellationToken = default)
    {
        var payment = await _context.PaymentTransactions
            .FirstOrDefaultAsync(p => p.Id == paymentTransactionId, cancellationToken);

        if (payment == null)
        {
            throw new KeyNotFoundException("Payment transaction not found.");
        }

        if (payment.Status != PaymentStatus.Completed)
        {
            throw new InvalidOperationException($"Cannot refund payment with status '{payment.Status}'.");
        }

        payment.Status = PaymentStatus.Refunded;
        payment.UpdatedAt = DateTime.UtcNow;

        if (payment.Purpose == PaymentPurpose.ClassroomEnrollment && payment.TeacherAmount.HasValue && payment.ClassroomId.HasValue)
        {
            var classroom = await _context.Classrooms
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == payment.ClassroomId.Value, cancellationToken);

            if (classroom != null)
            {
                var wallet = await _context.TeacherWallets
                    .FirstOrDefaultAsync(w => w.TeacherId == classroom.TeacherId, cancellationToken);

                if (wallet != null)
                {
                    var teacherDeduction = payment.TeacherAmount.Value;
                    wallet.EarnedBalance = Math.Max(0m, wallet.EarnedBalance - teacherDeduction);
                    wallet.UpdatedAt = DateTime.UtcNow;

                    var ledgerTx = new WalletTransaction
                    {
                        Id = Guid.NewGuid(),
                        TeacherId = classroom.TeacherId,
                        Type = WalletTransactionType.Refund,
                        Amount = -teacherDeduction,
                        BalanceType = WalletBalanceType.Earned,
                        ReferenceId = payment.Id,
                        Description = $"Refund reversal for payment '{payment.Id}'",
                        CreatedAt = DateTime.UtcNow
                    };
                    await _context.WalletTransactions.AddAsync(ledgerTx, cancellationToken);
                }
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
