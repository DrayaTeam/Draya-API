namespace Draya.Application.Exams.DTOs;

public record StudentExamSummaryDto(
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
    bool HasSubmitted = false,
    string AttemptStatus = "NotStarted",
    decimal? LatestScore = null,
    int UsedAttempts = 0
);
