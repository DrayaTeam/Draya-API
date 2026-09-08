using System;

namespace Draya.Domain.Exams;

public enum GradingStatus
{
    Pending,
    Grading,
    Completed,
    CompletedWithWarning,
    Failed
}

public class ExamGradingJob
{
    public Guid Id { get; private set; }
    public Guid StudentExamAttemptId { get; private set; }
    public string IdempotencyKey { get; private set; } = string.Empty;
    public GradingStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public string? ErrorMessage { get; private set; }

    private ExamGradingJob() { }

    public ExamGradingJob(Guid studentExamAttemptId, string idempotencyKey)
    {
        Id = Guid.NewGuid();
        StudentExamAttemptId = studentExamAttemptId;
        IdempotencyKey = idempotencyKey;
        Status = GradingStatus.Pending;
        CreatedAt = DateTime.UtcNow;
    }

    public void UpdateStatus(GradingStatus status, string? errorMessage = null)
    {
        Status = status;
        ErrorMessage = errorMessage;
        
        if (status is GradingStatus.Completed or GradingStatus.CompletedWithWarning or GradingStatus.Failed)
        {
            CompletedAt = DateTime.UtcNow;
        }
    }
}
