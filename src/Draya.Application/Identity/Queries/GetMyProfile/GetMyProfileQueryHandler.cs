using Draya.Application.Common.Interfaces;
using MediatR;

namespace Draya.Application.Identity.Queries.GetMyProfile;

public class GetMyProfileQueryHandler : IRequestHandler<GetMyProfileQuery, object>
{
    private readonly IIdentityService _identityService;

    public GetMyProfileQueryHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public Task<object> Handle(GetMyProfileQuery request, CancellationToken cancellationToken)
    {
        return _identityService.GetUserProfileAsync(request.UserId, cancellationToken);
    }
}
