using System;
using System.Collections.Generic;
using Draya.Application.Exams.Commands.Questions;

namespace Draya.Application.Exams.DTOs;

public class AddExamQuestionRequest
{
    public string Text { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Difficulty { get; set; } = string.Empty;
    public string? Rubric { get; set; }
    public string SourceChunkIds { get; set; } = string.Empty;
    public List<UpdateExamQuestionOptionDto>? Options { get; set; }
}

public class UpdateExamQuestionRequest
{
    public string Text { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Difficulty { get; set; } = string.Empty;
    public string? Rubric { get; set; }
    public List<UpdateExamQuestionOptionDto>? Options { get; set; }
}

public class RefineExamQuestionRequest
{
    public string Instruction { get; set; } = string.Empty;
}
