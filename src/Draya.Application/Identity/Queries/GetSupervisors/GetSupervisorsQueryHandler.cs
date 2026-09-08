using Draya.Application.Common.Interfaces;
using Draya.Application.Identity.DTOs;
using MediatR;

namespace Draya.Application.Identity.Queries.GetSupervisors;

public class GetSupervisorsQueryHandler : IRequestHandler<GetSupervisorsQuery, List<SupervisorDto>>
{
    private readonly IIdentityService _identityService;

    public GetSupervisorsQueryHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<List<SupervisorDto>> Handle(GetSupervisorsQuery request, CancellationToken cancellationToken)
    {
        return await _identityService.GetSupervisorsAsync(cancellationToken);
    }
}
