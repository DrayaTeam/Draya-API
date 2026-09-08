using Draya.Application.Common.Interfaces;
using MediatR;

namespace Draya.Application.Identity.Commands.UpdateAdminProfile;

public class UpdateAdminProfileCommandHandler : IRequestHandler<UpdateAdminProfileCommand>
{
    private readonly IIdentityService _identityService;

    public UpdateAdminProfileCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task Handle(UpdateAdminProfileCommand request, CancellationToken cancellationToken)
    {
        await _identityService.UpdateAdminProfileAsync(
            request.UserId,
            request.FullName,
            request.Email,
            request.PhoneNumber,
            cancellationToken);
    }
}
