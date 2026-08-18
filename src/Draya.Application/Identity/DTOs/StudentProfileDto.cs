namespace Draya.Application.Identity.DTOs;

public record StudentProfileDto(
    Guid UserId,
    string Email,
    string FullName,
    string ParentGuardianEmail,
    string ParentGuardianName,
    string ParentGuardianPhone,
    DateTime? DateOfBirth,
    string? ProfilePictureUrl = null
);

