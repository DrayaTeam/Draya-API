using Draya.Application.Classrooms.DTOs;
using MediatR;

namespace Draya.Application.Classrooms.Queries.GetTeacherClassrooms;

public record GetTeacherClassroomsQuery(
    Guid TeacherId,
    int Page,
    int PageSize,
    Guid? SubjectId = null,
    Guid? GradeLevelId = null,
    Guid? ClassroomTypeId = null
) : IRequest<PagedResult<ClassroomDto>>;
