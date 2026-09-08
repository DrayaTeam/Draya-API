namespace Draya.Domain.Payments;

public class PaymentTransaction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public PaymentPurpose Purpose { get; set; }
    public Guid PayerId { get; set; }
    public Guid? ClassroomId { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal? CommissionPercent { get; set; }
    public decimal? CommissionAmount { get; set; }
    public decimal? TeacherAmount { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public string RedirectionUrl { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
