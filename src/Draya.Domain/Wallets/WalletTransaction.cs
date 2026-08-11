namespace Draya.Domain.Wallets;

public class WalletTransaction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TeacherId { get; set; }
    public WalletTransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public WalletBalanceType BalanceType { get; set; }

    /// <summary>
    /// Soft reference (not an enforced foreign key).
    /// Points to PaymentTransaction.Id, Exam.Id, or WithdrawalRequest.Id depending on Type.
    /// </summary>
    public Guid? ReferenceId { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
