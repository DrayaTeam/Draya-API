namespace Draya.Application.Identity.DTOs;

public record AdminStudentDto(
    Guid UserId,
    string FullName,
    string Email,
    string ParentGuardianEmail,
    string ParentGuardianName,
    string ParentGuardianPhone,
    DateTime? DateOfBirth,
    bool IsActive,
    DateTime CreatedAt
);
