using System;
using System.Collections.Generic;

namespace Draya.Application.Exams.DTOs;

public record StudentExamDto(
    Guid Id,
    Guid ClassroomId,
    Guid SectionId,
    string Title,
    string Topic,
    int DurationMinutes,
    DateTime StartDate,
    DateTime? EndDate,
    int AllowedAttempts,
    DateTime CreatedAt,
    List<StudentExamQuestionDto> Questions
);

public record StudentExamQuestionDto(
    Guid Id,
    string Text,
    string Type,
    string Difficulty,
    string SourceChunkIds,
    string? Rubric,
    List<StudentExamQuestionOptionDto> Options
);

public record StudentExamQuestionOptionDto(
    Guid Id,
    string Text
);
