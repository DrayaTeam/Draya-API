namespace Draya.Domain.Subscriptions;

public class UsageCounter
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TeacherId { get; set; }
    public DateOnly PeriodMonth { get; set; }
    public int FreeExamsUsed { get; set; } = 0;
    public int PaidExamsGenerated { get; set; } = 0;
    public int TotalExamsGenerated => FreeExamsUsed + PaidExamsGenerated;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
