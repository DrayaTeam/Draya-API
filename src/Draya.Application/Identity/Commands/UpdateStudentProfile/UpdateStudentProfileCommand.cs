using MediatR;

namespace Draya.Application.Identity.Commands.UpdateStudentProfile;

public record UpdateStudentProfileCommand(
    Guid UserId,
    string FullName,
    string ParentGuardianEmail,
    string? ParentGuardianName,
    string? ParentGuardianPhone,
    DateTime? DateOfBirth
) : IRequest;
