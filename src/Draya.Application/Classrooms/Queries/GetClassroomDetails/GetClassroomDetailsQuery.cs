using Draya.Application.Classrooms.DTOs;
using MediatR;

namespace Draya.Application.Classrooms.Queries.GetClassroomDetails;

public record GetClassroomDetailsQuery(
    Guid ClassroomId,
    Guid UserId,
    string UserRole
) : IRequest<ClassroomDto>;
