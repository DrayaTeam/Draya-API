namespace Draya.Domain.Wallets;

public class TeacherPayoutAccount
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TeacherId { get; set; }
    public PayoutAccountType AccountType { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public string AccountIdentifier { get; set; } = string.Empty;
    public bool IsDefault { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
