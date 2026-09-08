namespace Draya.Api.Controllers.Identity.Requests;

public record UpdateTeacherProfileRequest(
    string FullName,
    string? Phone,
    string? Specialization,
    string? Description
);

public record UpdateStudentProfileRequest(
    string FullName,
    string ParentGuardianEmail,
    string? ParentGuardianName,
    string? ParentGuardianPhone,
    DateTime? DateOfBirth
);
