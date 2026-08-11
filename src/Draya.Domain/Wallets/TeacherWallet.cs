namespace Draya.Domain.Wallets;

public class TeacherWallet
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TeacherId { get; set; }
    public decimal EarnedBalance { get; set; } = 0m;
    public decimal PurchasedBalance { get; set; } = 0m;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
