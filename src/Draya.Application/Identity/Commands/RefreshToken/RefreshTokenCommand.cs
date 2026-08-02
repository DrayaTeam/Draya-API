using Draya.Application.Identity.DTOs;
using MediatR;

namespace Draya.Application.Identity.Commands.RefreshToken;

public record RefreshTokenCommand(string Token) : IRequest<AuthResponseDto>;
