using Draya.Application.Common.Interfaces;
using MediatR;

namespace Draya.Application.Identity.Commands.ResendSupervisorInvite;

public class ResendSupervisorInviteCommandHandler : IRequestHandler<ResendSupervisorInviteCommand>
{
    private readonly IIdentityService _identityService;

    public ResendSupervisorInviteCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task Handle(ResendSupervisorInviteCommand request, CancellationToken cancellationToken)
    {
        await _identityService.ResendSupervisorInviteAsync(request.SupervisorId, cancellationToken);
    }
}
