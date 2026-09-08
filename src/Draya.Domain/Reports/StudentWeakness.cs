using System;

namespace Draya.Domain.Reports;

public class StudentWeakness
{
    public Guid Id { get; private set; }
    public Guid StudentId { get; private set; }
    public Guid TopicId { get; private set; }
    public string TopicNameSnapshot { get; private set; }
    public decimal CurrentProficiencyPercent { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime LastUpdatedAt { get; private set; }

    private StudentWeakness() { }

    public StudentWeakness(Guid studentId, Guid topicId, string topicNameSnapshot, decimal initialProficiencyPercent)
    {
        Id = Guid.NewGuid();
        StudentId = studentId;
        TopicId = topicId;
        TopicNameSnapshot = topicNameSnapshot;
        CurrentProficiencyPercent = initialProficiencyPercent;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
        LastUpdatedAt = DateTime.UtcNow;
    }

    public void UpdatePerformance(decimal newProficiencyPercent, decimal masteryThresholdPercent, bool hasPendingTeacherReview)
    {
        CurrentProficiencyPercent = newProficiencyPercent;
        LastUpdatedAt = DateTime.UtcNow;

        if (hasPendingTeacherReview)
        {
            // Do not resolve if there is a pending review, but we can update proficiency.
            return;
        }

        if (CurrentProficiencyPercent >= masteryThresholdPercent)
        {
            IsActive = false;
        }
        else
        {
            // Re-activate if proficiency drops below mastery threshold
            IsActive = true;
        }
    }
}
