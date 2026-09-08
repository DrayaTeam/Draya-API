namespace Draya.Domain.Wallets;

public class WithdrawalRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TeacherId { get; set; }
    public Guid? PayoutAccountId { get; set; }
    public decimal Amount { get; set; }
    public WithdrawalStatus Status { get; set; } = WithdrawalStatus.Pending;
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
    public string? AdminNote { get; set; }
    public string? RejectionReason { get; set; }
}
