using MediatR;

namespace Draya.Application.Identity.Commands.AcceptSupervisorInvite;

public record AcceptSupervisorInviteCommand(string Email, string Token, string NewPassword, string ConfirmPassword) : IRequest;
