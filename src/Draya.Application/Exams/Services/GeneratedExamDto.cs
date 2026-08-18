using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Draya.Application.Exams.Services;

public class GeneratedExamDto
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("questions")]
    public List<GeneratedQuestionDto> Questions { get; set; } = new();
}

public class GeneratedQuestionDto
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty; // MultipleChoice, TrueFalse

    [JsonPropertyName("difficulty")]
    public string Difficulty { get; set; } = string.Empty;

    [JsonPropertyName("sourceChunkIds")]
    public List<string> SourceChunkIds { get; set; } = new();

    [JsonPropertyName("options")]
    public List<GeneratedQuestionOptionDto>? Options { get; set; }

    [JsonPropertyName("correctAnswerIndex")]
    public int? CorrectAnswerIndex { get; set; }
}

public class GeneratedQuestionOptionDto
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;
}
