using Draya.Application.Classrooms.DTOs;
using MediatR;

namespace Draya.Application.Classrooms.Queries.GetTeacherClassrooms;

public record GetTeacherClassroomsQuery(
    Guid TeacherId,
    int Page,
    int PageSize
) : IRequest<PagedResult<ClassroomDto>>;
