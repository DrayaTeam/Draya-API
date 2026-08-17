using MediatR;

namespace Draya.Application.Identity.Commands.ResendSupervisorInvite;

public record ResendSupervisorInviteCommand(Guid SupervisorId) : IRequest;
