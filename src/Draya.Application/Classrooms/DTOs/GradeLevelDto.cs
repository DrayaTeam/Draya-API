namespace Draya.Application.Classrooms.DTOs;

public record GradeLevelDto(
    Guid Id,
    string Name,
    string Description,
    int SortOrder,
    bool IsActive,
    DateTime CreatedAt
);
