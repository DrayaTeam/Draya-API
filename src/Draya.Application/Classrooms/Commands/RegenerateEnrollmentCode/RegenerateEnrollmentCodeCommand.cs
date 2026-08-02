using Draya.Application.Classrooms.DTOs;
using MediatR;

namespace Draya.Application.Classrooms.Commands.RegenerateEnrollmentCode;

public record RegenerateEnrollmentCodeCommand(
    Guid ClassroomId,
    Guid TeacherId
) : IRequest<ClassroomDto>;
