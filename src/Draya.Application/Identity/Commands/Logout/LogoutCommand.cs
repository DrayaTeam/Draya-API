using MediatR;

namespace Draya.Application.Identity.Commands.Logout;

public record LogoutCommand(Guid UserId, string RefreshToken) : IRequest;
