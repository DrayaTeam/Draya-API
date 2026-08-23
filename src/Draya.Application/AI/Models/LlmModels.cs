using System.Collections.Generic;

namespace Draya.Application.AI.Models;

public enum AiFeature
{
    ExamGeneration,
    ExamGrading,
    QuestionRefinement,
    ReportGeneration
}

public class LlmRequest
{
    public AiFeature Feature { get; set; } = AiFeature.ExamGeneration;
    public string SystemPrompt { get; set; } = string.Empty;
    public string UserPrompt { get; set; } = string.Empty;
    public bool RequestJsonResponse { get; set; } = true;
}

public class LlmResponse
{
    public string Content { get; set; } = string.Empty;
    public int? InputTokens { get; set; }
    public int? OutputTokens { get; set; }
}
