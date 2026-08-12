namespace Draya.Application.Classrooms.DTOs;

public record ClassroomTypeDto(
    Guid Id,
    string Name,
    string Description,
    bool IsActive,
    DateTime CreatedAt
);
