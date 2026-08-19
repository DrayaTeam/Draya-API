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

    public string? Rubric { get; private set; }

    private readonly List<ExamQuestionOption> _options = new();
    public IReadOnlyCollection<ExamQuestionOption> Options => _options.AsReadOnly();

    private ExamQuestion() { }

    public ExamQuestion(Guid examId, string text, string type, string difficulty, string sourceChunkIds, string? rubric = null)
    {
        Id = Guid.NewGuid();
        ExamId = examId;
        Text = text;
        Type = type;
        Difficulty = difficulty;
        SourceChunkIds = sourceChunkIds;
        Rubric = rubric;
    }

    public void AddOption(ExamQuestionOption option)
    {
        _options.Add(option);
    }

    public void Update(string text, string type, string difficulty, string? rubric)
    {
        Text = text;
        Type = type;
        Difficulty = difficulty;
        Rubric = rubric;
    }

    public void UpdateSourceChunks(string sourceChunkIds)
    {
        SourceChunkIds = sourceChunkIds;
    }

    public void ClearOptions()
    {
        _options.Clear();
    }
}
