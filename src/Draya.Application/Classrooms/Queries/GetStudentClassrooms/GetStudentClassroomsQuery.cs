using Draya.Application.Classrooms.DTOs;
using MediatR;

namespace Draya.Application.Classrooms.Queries.GetStudentClassrooms;

public record GetStudentClassroomsQuery(
    Guid StudentId, 
    int Page, 
    int PageSize,
    Guid? SubjectId = null,
    Guid? GradeLevelId = null,
    Guid? ClassroomTypeId = null) : IRequest<PagedResult<ClassroomDto>>;
