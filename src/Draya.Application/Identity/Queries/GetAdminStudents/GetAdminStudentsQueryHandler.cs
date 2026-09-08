using Draya.Application.Classrooms.DTOs;
using Draya.Application.Common.Interfaces;
using Draya.Application.Identity.DTOs;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Draya.Application.Identity.Queries.GetAdminStudents;

public class GetAdminStudentsQueryHandler : IRequestHandler<GetAdminStudentsQuery, PagedResult<AdminStudentDto>>
{
    private readonly IIdentityService _identityService;

    public GetAdminStudentsQueryHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<PagedResult<AdminStudentDto>> Handle(GetAdminStudentsQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _identityService.SearchStudentsForAdminAsync(
            request.SearchTerm,
            request.Page,
            request.PageSize,
            cancellationToken);

        var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);

        return new PagedResult<AdminStudentDto>(
            items,
            request.Page,
            request.PageSize,
            totalCount,
            totalPages
        );
    }
}
