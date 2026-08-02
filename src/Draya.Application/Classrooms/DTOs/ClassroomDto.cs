namespace Draya.Application.Classrooms.DTOs;

public record ClassroomDto(
    Guid ClassroomId,
    Guid TeacherId,
    string SubjectName,
    string Name,
    string EnrollmentCode,
    bool IsActive,
    int StudentCount,
    DateTime CreatedAt
);
