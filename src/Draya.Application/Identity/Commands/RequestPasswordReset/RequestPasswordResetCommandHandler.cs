using Draya.Application.Common.Interfaces;
using Draya.Application.Identity.DTOs;
using MediatR;

namespace Draya.Application.Identity.Commands.RequestPasswordReset;

public class RequestPasswordResetCommandHandler : IRequestHandler<RequestPasswordResetCommand, PasswordResetRequestResponseDto>
{
    private readonly IIdentityService _identityService;

    public RequestPasswordResetCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public Task<PasswordResetRequestResponseDto> Handle(RequestPasswordResetCommand request, CancellationToken cancellationToken)
    {
        return _identityService.RequestPasswordResetAsync(request.Email, cancellationToken);
    }
}
