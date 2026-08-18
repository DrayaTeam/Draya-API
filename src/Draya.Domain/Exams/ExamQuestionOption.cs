using System;

namespace Draya.Domain.Exams;

public class ExamQuestionOption
{
    public Guid Id { get; private set; }
    public Guid ExamQuestionId { get; private set; }
    public string Text { get; private set; } = string.Empty;
    public bool IsCorrect { get; private set; }

    private ExamQuestionOption() { }

    public ExamQuestionOption(Guid examQuestionId, string text, bool isCorrect)
    {
        Id = Guid.NewGuid();
        ExamQuestionId = examQuestionId;
        Text = text;
        IsCorrect = isCorrect;
    }
}
