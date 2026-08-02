using Draya.Application.Identity.DTOs;
using MediatR;

namespace Draya.Application.Identity.Commands.RequestPasswordReset;

public record RequestPasswordResetCommand(string Email) : IRequest<PasswordResetRequestResponseDto>;
