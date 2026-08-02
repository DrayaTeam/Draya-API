using MediatR;

namespace Draya.Application.Classrooms.Commands.DeactivateClassroom;

public record DeactivateClassroomCommand(
    Guid ClassroomId,
    Guid TeacherId
) : IRequest;
