namespace Draya.Domain.Subscriptions;
public class UsageCounter { public Guid Id { get; set; } = Guid.NewGuid(); public Guid TeacherId { get; set; } public DateOnly PeriodMonth { get; set; } public int CurrentStudentsCount { get; set; } public int ExamsGeneratedCount { get; set; } public decimal StorageUsedMB { get; set; } }
