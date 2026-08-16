namespace Draya.Application.Identity.DTOs;

public record StudentProfileDto(
    Guid UserId,
    string Email,
    string FullName,
    string ParentGuardianEmail,
    DateTime? DateOfBirth,
    string? ProfilePictureUrl = null
);

