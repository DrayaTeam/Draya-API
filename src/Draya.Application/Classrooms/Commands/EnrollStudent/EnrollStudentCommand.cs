using Draya.Application.Classrooms.DTOs;
using MediatR;

namespace Draya.Application.Classrooms.Commands.EnrollStudent;

public record EnrollStudentCommand(
    Guid StudentId,
    string EnrollmentCode
) : IRequest<ClassroomDto>;
