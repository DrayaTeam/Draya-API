using System;
using System.Collections.Generic;

namespace Draya.Domain.Exams;

public class Exam
{
    public Guid Id { get; private set; }
    public Guid ClassroomId { get; private set; }
    public Guid SectionId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Topic { get; private set; } = string.Empty;
    public int DurationMinutes { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime? EndDate { get; private set; }
    public int AllowedAttempts { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private readonly List<ExamQuestion> _questions = new();
    public IReadOnlyCollection<ExamQuestion> Questions => _questions.AsReadOnly();

    private Exam() { }

    public Exam(Guid classroomId, Guid sectionId, string title, string topic, int durationMinutes, DateTime startDate, DateTime? endDate = null, int allowedAttempts = 1)
    {
        Id = Guid.NewGuid();
        ClassroomId = classroomId;
        SectionId = sectionId;
        Title = title;
        Topic = topic;
        DurationMinutes = durationMinutes;
        StartDate = startDate;
        EndDate = endDate;
        AllowedAttempts = allowedAttempts;
        CreatedAt = DateTime.UtcNow;
    }

    public void AddQuestion(ExamQuestion question)
    {
        _questions.Add(question);
    }

    public void RemoveQuestion(Guid questionId)
    {
        var q = _questions.Find(x => x.Id == questionId);
        if (q != null)
        {
            _questions.Remove(q);
        }
    }
}
