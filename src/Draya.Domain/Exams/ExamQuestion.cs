using System;
using System.Collections.Generic;

namespace Draya.Domain.Exams;

public class ExamQuestion
{
    public Guid Id { get; private set; }
    public Guid ExamId { get; private set; }
    public string Text { get; private set; } = string.Empty;
    public string Type { get; private set; } = string.Empty;
    public string Difficulty { get; private set; } = string.Empty;
    
    // For AI Grounding Validation tracking
    public string SourceChunkIds { get; private set; } = string.Empty;

    private readonly List<ExamQuestionOption> _options = new();
    public IReadOnlyCollection<ExamQuestionOption> Options => _options.AsReadOnly();

    private ExamQuestion() { }

    public ExamQuestion(Guid examId, string text, string type, string difficulty, string sourceChunkIds)
    {
        Id = Guid.NewGuid();
        ExamId = examId;
        Text = text;
        Type = type;
        Difficulty = difficulty;
        SourceChunkIds = sourceChunkIds;
    }

    public void AddOption(ExamQuestionOption option)
    {
        _options.Add(option);
    }
}
