namespace Draya.Application.Classrooms.DTOs;

public record StudentProgressDto(
    int CompletedLessons,
    int TotalLessons,
    int ProgressPercent,
    DateTime? LastAccessedAt
);
