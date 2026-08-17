using Draya.Application.Common.Interfaces;
using MediatR;

namespace Draya.Application.Identity.Commands.ToggleSupervisorStatus;

public class ToggleSupervisorStatusCommandHandler : IRequestHandler<ToggleSupervisorStatusCommand>
{
    private readonly IIdentityService _identityService;

    public ToggleSupervisorStatusCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task Handle(ToggleSupervisorStatusCommand request, CancellationToken cancellationToken)
    {
        await _identityService.ToggleSupervisorStatusAsync(request.SupervisorId, request.IsActive, cancellationToken);
    }
}
