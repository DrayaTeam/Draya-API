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
    public DateTime CreatedAt { get; private set; }

    private readonly List<ExamQuestion> _questions = new();
    public IReadOnlyCollection<ExamQuestion> Questions => _questions.AsReadOnly();

    private Exam() { }

    public Exam(Guid classroomId, Guid sectionId, string title, string topic)
    {
        Id = Guid.NewGuid();
        ClassroomId = classroomId;
        SectionId = sectionId;
        Title = title;
        Topic = topic;
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
