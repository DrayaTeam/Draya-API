using Draya.Application.Classrooms.DTOs;
using MediatR;

namespace Draya.Application.Classrooms.Queries.GetClassroomsByTeacher;

public record GetClassroomsByTeacherQuery(Guid TeacherId, int Page, int PageSize) : IRequest<PagedResult<ClassroomDto>>;
