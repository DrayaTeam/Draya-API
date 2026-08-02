using Draya.Application.Identity.DTOs;
using MediatR;

namespace Draya.Application.Identity.Commands.Login;

public record LoginCommand(
    string Email,
    string Password
) : IRequest<AuthResponseDto>;
