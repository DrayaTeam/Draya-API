using Draya.Application.Common.Interfaces;
using Draya.Application.Identity.DTOs;
using MediatR;

namespace Draya.Application.Identity.Commands.InviteSupervisor;

public class InviteSupervisorCommandHandler : IRequestHandler<InviteSupervisorCommand, SupervisorDto>
{
    private readonly IIdentityService _identityService;

    public InviteSupervisorCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<SupervisorDto> Handle(InviteSupervisorCommand request, CancellationToken cancellationToken)
    {
        return await _identityService.InviteSupervisorAsync(request.Name, request.Email, request.Role, cancellationToken);
    }
}
