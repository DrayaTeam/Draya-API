using System;
using System.Collections.Generic;

namespace Draya.Application.Exams.DTOs;

public record ExamDto(
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
    List<ExamQuestionDto> Questions
);

public record ExamQuestionDto(
    Guid Id,
    string Text,
    string Type,
    string Difficulty,
    string SourceChunkIds,
    string? Rubric,
    List<ExamQuestionOptionDto> Options
);

public record ExamQuestionOptionDto(
    Guid Id,
    string Text,
    bool IsCorrect
);
