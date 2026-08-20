using Draya.Application.Classrooms.DTOs;
using Draya.Application.Identity.DTOs;
using MediatR;

namespace Draya.Application.Identity.Queries.GetAdminStudents;

public record GetAdminStudentsQuery(
    string? SearchTerm,
    int Page = 1,
    int PageSize = 20
) : IRequest<PagedResult<AdminStudentDto>>;
