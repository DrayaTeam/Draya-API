using MediatR;

namespace Draya.Application.Identity.Commands.UpdateAdminProfile;

public record UpdateAdminProfileCommand(
    Guid UserId,
    string FullName,
    string Email,
    string? PhoneNumber
) : IRequest;
