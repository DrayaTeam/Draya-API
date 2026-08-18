using System;

namespace Draya.Domain.Exams;

public enum GenerationStatus
{
    Pending,
    Retrieving,
    Generating,
    Validating,
    Completed,
    CompletedWithWarning,
    DataUnavailable,
    Failed
}

public class ExamGeneration
{
    public Guid Id { get; private set; }
    public string IdempotencyKey { get; private set; } = string.Empty;
    public Guid TeacherId { get; private set; }
    public Guid ClassroomId { get; private set; }
    public Guid SectionId { get; private set; }
    public int RequestedCount { get; private set; }
    public int GeneratedCount { get; private set; }
    public GenerationStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public string? ErrorMessage { get; private set; }
    public Guid? ExamId { get; private set; }

    private ExamGeneration() { }

    public ExamGeneration(Guid teacherId, Guid classroomId, Guid sectionId, int requestedCount, string idempotencyKey)
    {
        Id = Guid.NewGuid();
        TeacherId = teacherId;
        ClassroomId = classroomId;
        SectionId = sectionId;
        RequestedCount = requestedCount;
        IdempotencyKey = idempotencyKey;
        Status = GenerationStatus.Pending;
        CreatedAt = DateTime.UtcNow;
    }

    public void UpdateStatus(GenerationStatus status, string? errorMessage = null)
    {
        Status = status;
        ErrorMessage = errorMessage;
        
        if (status is GenerationStatus.Completed or GenerationStatus.CompletedWithWarning or GenerationStatus.Failed or GenerationStatus.DataUnavailable)
        {
            CompletedAt = DateTime.UtcNow;
        }
    }

    public void SetGeneratedCount(int count)
    {
        GeneratedCount = count;
    }

    public void SetExamId(Guid examId)
    {
        ExamId = examId;
    }
}
