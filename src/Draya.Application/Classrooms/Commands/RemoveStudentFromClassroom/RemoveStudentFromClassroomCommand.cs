using MediatR;

namespace Draya.Application.Classrooms.Commands.RemoveStudentFromClassroom;

public record RemoveStudentFromClassroomCommand(
    Guid ClassroomId,
    Guid StudentId,
    Guid TeacherId
) : IRequest;
