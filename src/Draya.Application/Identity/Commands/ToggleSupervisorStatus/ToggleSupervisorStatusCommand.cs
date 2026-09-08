using MediatR;

namespace Draya.Application.Identity.Commands.ToggleSupervisorStatus;

public record ToggleSupervisorStatusCommand(Guid SupervisorId, bool IsActive) : IRequest;
