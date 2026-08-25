using System;
using System.Collections.Generic;
using System.Linq;

namespace Draya.Domain.Reports;

public class PerformanceReport
{
    public Guid Id { get; private set; }
    public Guid StudentId { get; private set; }
    public Guid ExamAttemptId { get; private set; }
    public Guid TeacherId { get; private set; }
    public string TeacherName { get; private set; } = string.Empty;
    public string SummaryText { get; private set; } = string.Empty;
    public int TotalQuestionsAsked { get; private set; } = 0;
    public int TotalQuestionsReplied { get; private set; } = 0;
    public decimal AverageExamDurationMinutes { get; private set; } = 0;
    public int CompletedLessons { get; private set; } = 0;
    public decimal ClassroomPercentile { get; private set; } = 0;
    public DateTime GeneratedAt { get; private set; }
    public bool IsApproved { get; private set; }

    private readonly List<SubjectProficiency> _subjectProficiencies = new();
    public IReadOnlyCollection<SubjectProficiency> SubjectProficiencies => _subjectProficiencies.AsReadOnly();

    private readonly List<TrendPoint> _trendPoints = new();
    public IReadOnlyCollection<TrendPoint> TrendPoints => _trendPoints.AsReadOnly();

    private readonly List<WeakTopic> _weakTopics = new();
    public IReadOnlyCollection<WeakTopic> WeakTopics => _weakTopics.AsReadOnly();

    private PerformanceReport() { } // EF Core

    public PerformanceReport(Guid studentId, Guid examAttemptId, Guid teacherId, string teacherName)
    {
        Id = Guid.NewGuid();
        StudentId = studentId;
        ExamAttemptId = examAttemptId;
        TeacherId = teacherId;
        TeacherName = teacherName;
        GeneratedAt = DateTime.UtcNow;
        IsApproved = false;
    }

    public void AddSubjectProficiencies(IEnumerable<SubjectProficiency> proficiencies)
    {
        _subjectProficiencies.AddRange(proficiencies);
    }

    public void AddTrendPoints(IEnumerable<TrendPoint> points)
    {
        _trendPoints.AddRange(points);
    }

    public void AddWeakTopics(IEnumerable<WeakTopic> topics)
    {
        _weakTopics.AddRange(topics);
    }

    public void SetAiSummary(string summaryText)
    {
        SummaryText = summaryText;
    }

    public void SetMetrics(int questionsAsked, int questionsReplied, decimal avgExamDuration, int completedLessons, decimal percentile)
    {
        TotalQuestionsAsked = questionsAsked;
        TotalQuestionsReplied = questionsReplied;
        AverageExamDurationMinutes = avgExamDuration;
        CompletedLessons = completedLessons;
        ClassroomPercentile = percentile;
    }

    public void Approve()
    {
        IsApproved = true;
    }
}

public record SubjectProficiency(string SubjectName, decimal ProficiencyPercent);

public record TrendPoint(DateTime Month, decimal AverageScore);

public record WeakTopic(string TopicName, string SubjectName, decimal ProficiencyPercent, string Status, string Recommendation);
