using System;
using System.Collections.Generic;

namespace Draya.Domain.Exams;

public class StudentExamAttempt
{
    public Guid Id { get; private set; }
    public Guid ExamId { get; private set; }
    public Guid StudentId { get; private set; }
    public DateTime StartedAt { get; private set; }
    public DateTime? SubmittedAt { get; private set; }
    public bool IsSubmitted { get; private set; }
    
    // Grading info
    public decimal? FinalScore { get; private set; }
    public decimal? MaxScore { get; private set; }
    public bool NeedsTeacherReview { get; private set; }
    
    private readonly List<StudentAnswer> _answers = new();
    public IReadOnlyCollection<StudentAnswer> Answers => _answers.AsReadOnly();

    private StudentExamAttempt() { }

    public StudentExamAttempt(Guid examId, Guid studentId)
    {
        Id = Guid.NewGuid();
        ExamId = examId;
        StudentId = studentId;
        StartedAt = DateTime.UtcNow;
        IsSubmitted = false;
    }

    public void Submit()
    {
        IsSubmitted = true;
        SubmittedAt = DateTime.UtcNow;
    }

    public void AddAnswer(StudentAnswer answer)
    {
        _answers.Add(answer);
    }

    public void UpdateFinalScore(decimal score, decimal maxScore, bool needsReview)
    {
        FinalScore = score;
        MaxScore = maxScore;
        NeedsTeacherReview = needsReview;
    }
}
