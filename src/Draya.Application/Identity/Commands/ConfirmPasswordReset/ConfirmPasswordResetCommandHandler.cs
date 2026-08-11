using Draya.Application.Common.Interfaces;
using MediatR;

namespace Draya.Application.Identity.Commands.ConfirmPasswordReset;

public class ConfirmPasswordResetCommandHandler : IRequestHandler<ConfirmPasswordResetCommand>
{
    private readonly IIdentityService _identityService;

    public ConfirmPasswordResetCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public Task Handle(ConfirmPasswordResetCommand request, CancellationToken cancellationToken)
    {
        return _identityService.ConfirmPasswordResetAsync(request.Token, request.NewPassword, cancellationToken);
    }
}
