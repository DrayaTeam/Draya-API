using Draya.Application.Classrooms.DTOs;
using MediatR;

namespace Draya.Application.Classrooms.Commands.UpdateClassroom;

public record UpdateClassroomCommand(
    Guid ClassroomId,
    Guid TeacherId,
    string Name,
    Guid SubjectId,
    bool IsActive
) : IRequest<ClassroomDto>;
