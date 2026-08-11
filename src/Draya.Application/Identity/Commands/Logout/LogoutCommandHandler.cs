using Draya.Application.Common.Interfaces;
using MediatR;

namespace Draya.Application.Identity.Commands.Logout;

public class LogoutCommandHandler : IRequestHandler<LogoutCommand>
{
    private readonly IIdentityService _identityService;

    public LogoutCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public Task Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        return _identityService.LogoutAsync(request.UserId, request.RefreshToken, cancellationToken);
    }
}
