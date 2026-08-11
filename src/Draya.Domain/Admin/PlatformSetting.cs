namespace Draya.Domain.Admin;

public class PlatformSetting
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public decimal AIExamPrice { get; set; } = 20.00m;
    public int FreeMonthlyAIExamQuota { get; set; } = 3;
    public decimal PlatformCommissionPercent { get; set; } = 5.00m;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Guid? UpdatedByAdminId { get; set; }
}
