using MediatR;

namespace Draya.Application.Identity.Commands.UpdateStudentProfile;

public record UpdateStudentProfileCommand(
    Guid UserId,
    string FullName,
    string ParentGuardianEmail,
    DateTime? DateOfBirth
) : IRequest;
