using System;

namespace Draya.Domain.Exams;

public class AnswerGradingResult
{
    public Guid Id { get; private set; }
    public Guid StudentAnswerId { get; private set; }
    public decimal Score { get; private set; }
    public decimal MaxScore { get; private set; }
    public decimal? ConfidenceScore { get; private set; }
    public string? Rationale { get; private set; }
    public bool IsAiGraded { get; private set; }
    public bool NeedsTeacherReview { get; private set; }
    public decimal? TeacherOverrideScore { get; private set; }

    private AnswerGradingResult() { }

    public AnswerGradingResult(Guid studentAnswerId, decimal score, decimal maxScore, decimal? confidenceScore, string? rationale, bool isAiGraded, bool needsTeacherReview)
    {
        Id = Guid.NewGuid();
        StudentAnswerId = studentAnswerId;
        Score = score;
        MaxScore = maxScore;
        ConfidenceScore = confidenceScore;
        Rationale = rationale;
        IsAiGraded = isAiGraded;
        NeedsTeacherReview = needsTeacherReview;
    }

    public void OverrideScore(decimal overrideScore)
    {
        TeacherOverrideScore = overrideScore;
        NeedsTeacherReview = false; // Resolved
    }

    public decimal GetFinalScore() => TeacherOverrideScore ?? Score;
}
