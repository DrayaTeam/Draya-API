using System;

namespace Draya.Domain.Reports;

public class StudentWeaknessHistory
{
    public Guid Id { get; private set; }
    public Guid StudentWeaknessId { get; private set; }
    public decimal PreviousProficiencyPercent { get; private set; }
    public decimal NewProficiencyPercent { get; private set; }
    public bool PreviousIsActive { get; private set; }
    public bool NewIsActive { get; private set; }
    public Guid? SourceAttemptId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private StudentWeaknessHistory() { }

    public StudentWeaknessHistory(
        Guid studentWeaknessId, 
        decimal previousProficiencyPercent, 
        decimal newProficiencyPercent, 
        bool previousIsActive, 
        bool newIsActive, 
        Guid? sourceAttemptId)
    {
        Id = Guid.NewGuid();
        StudentWeaknessId = studentWeaknessId;
        PreviousProficiencyPercent = previousProficiencyPercent;
        NewProficiencyPercent = newProficiencyPercent;
        PreviousIsActive = previousIsActive;
        NewIsActive = newIsActive;
        SourceAttemptId = sourceAttemptId;
        CreatedAt = DateTime.UtcNow;
    }
}
