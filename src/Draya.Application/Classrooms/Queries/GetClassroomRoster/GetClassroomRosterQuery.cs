using Draya.Application.Classrooms.DTOs;
using MediatR;

namespace Draya.Application.Classrooms.Queries.GetClassroomRoster;

public record GetClassroomRosterQuery(
    Guid ClassroomId,
    Guid TeacherId,
    int Page,
    int PageSize
) : IRequest<PagedResult<StudentRosterItemDto>>;
