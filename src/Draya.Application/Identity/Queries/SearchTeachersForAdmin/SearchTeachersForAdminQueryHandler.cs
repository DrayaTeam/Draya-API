using Draya.Application.Common.Interfaces;
using Draya.Application.Identity.DTOs;
using MediatR;

namespace Draya.Application.Identity.Queries.SearchTeachersForAdmin;

public class SearchTeachersForAdminQueryHandler : IRequestHandler<SearchTeachersForAdminQuery, List<TeacherSearchDto>>
{
    private readonly IIdentityService _identityService;

    public SearchTeachersForAdminQueryHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<List<TeacherSearchDto>> Handle(SearchTeachersForAdminQuery request, CancellationToken cancellationToken)
    {
        return await _identityService.SearchTeachersForAdminAsync(request.Query, cancellationToken);
    }
}
