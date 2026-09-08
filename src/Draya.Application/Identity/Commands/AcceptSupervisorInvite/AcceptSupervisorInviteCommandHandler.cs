using Draya.Application.Common.Interfaces;
using MediatR;

namespace Draya.Application.Identity.Commands.AcceptSupervisorInvite;

public class AcceptSupervisorInviteCommandHandler : IRequestHandler<AcceptSupervisorInviteCommand>
{
    private readonly IIdentityService _identityService;

    public AcceptSupervisorInviteCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task Handle(AcceptSupervisorInviteCommand request, CancellationToken cancellationToken)
    {
        if (request.NewPassword != request.ConfirmPassword)
        {
            throw new ArgumentException("Passwords do not match.");
        }

        await _identityService.AcceptSupervisorInviteAsync(request.Email, request.Token, request.NewPassword, cancellationToken);
    }
}
