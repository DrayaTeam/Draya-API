namespace Draya.Domain.Classrooms;

public class Enrollment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid StudentId { get; set; }
    public Guid ClassroomId { get; set; }
    public Guid? PaymentTransactionId { get; set; }
    public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;
    public int CompletedLessons { get; set; } = 0;
    public DateTime? LastAccessedAt { get; set; }
    public EnrollmentStatus Status { get; set; } = EnrollmentStatus.Active;
}

public enum EnrollmentStatus
{
    Active,
    Unenrolled,
    Revoked
}
