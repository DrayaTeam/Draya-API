using MediatR;

namespace Draya.Application.Identity.Commands.ConfirmPasswordReset;

public record ConfirmPasswordResetCommand(string Token, string NewPassword) : IRequest;
