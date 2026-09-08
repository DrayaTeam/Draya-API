namespace Draya.Application.Identity.DTOs;

public record TeacherProfileDto(
    Guid UserId,
    string Email,
    string FullName,
    string? Phone,
    string? Specialization,
    string? Description = null,
    string? ProfilePictureUrl = null,
    int ClassroomsCount = 0,
    int StudentsCount = 0,
    int LessonsCount = 0,
    double? AverageRating = null
);

