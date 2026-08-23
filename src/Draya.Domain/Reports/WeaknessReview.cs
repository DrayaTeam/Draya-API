using System;

namespace Draya.Domain.Reports;

public class WeaknessReview
{
    public Guid Id { get; private set; }
    public Guid StudentWeaknessId { get; private set; }
    public decimal ProficiencyAtGeneration { get; private set; }
    public string AiExplanation { get; private set; }
    public string KeyConcepts { get; private set; }
    public string CommonMistakes { get; private set; }
    public string Recommendations { get; private set; }
    public DateTime GeneratedAt { get; private set; }
    public bool IsOutdated { get; private set; }

    private WeaknessReview() { }

    public WeaknessReview(
        Guid studentWeaknessId, 
        decimal proficiencyAtGeneration, 
        string aiExplanation, 
        string keyConcepts, 
        string commonMistakes, 
        string recommendations)
    {
        Id = Guid.NewGuid();
        StudentWeaknessId = studentWeaknessId;
        ProficiencyAtGeneration = proficiencyAtGeneration;
        AiExplanation = aiExplanation;
        KeyConcepts = keyConcepts;
        CommonMistakes = commonMistakes;
        Recommendations = recommendations;
        GeneratedAt = DateTime.UtcNow;
        IsOutdated = false;
    }

    public void MarkOutdated()
    {
        IsOutdated = true;
    }
}
