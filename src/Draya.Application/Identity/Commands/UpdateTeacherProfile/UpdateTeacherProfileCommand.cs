using MediatR;

namespace Draya.Application.Identity.Commands.UpdateTeacherProfile;

public record UpdateTeacherProfileCommand(
    Guid UserId,
    string FullName,
    string? Phone,
    string? Specialization,
    string? Description
) : IRequest;
