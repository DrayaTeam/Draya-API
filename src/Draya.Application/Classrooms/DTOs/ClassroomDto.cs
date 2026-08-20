namespace Draya.Application.Classrooms.DTOs;

public record ClassroomDto(
    Guid ClassroomId,
    Guid TeacherId,
    string SubjectName,
    string Name,
    string EnrollmentCode,
    bool IsActive,
    int StudentCount,
    DateTime CreatedAt,
    string ClassroomTypeName,
    string GradeLevelName,
    DateTime StartDate,
    DateTime EndDate,
    decimal Price,
    string? ImageUrl,
    int MaterialsCount = 0,
    StudentProgressDto? StudentProgress = null,
    string? TeacherName = null,
    string? TeacherAvatarUrl = null
);
