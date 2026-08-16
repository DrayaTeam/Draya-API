namespace Draya.Application.Identity.DTOs;

public record TeacherProfileDto(
    Guid UserId,
    string Email,
    string FullName,
    string? Phone,
    string? Specialization,
    string? Description = null,
    string? ProfilePictureUrl = null
);

