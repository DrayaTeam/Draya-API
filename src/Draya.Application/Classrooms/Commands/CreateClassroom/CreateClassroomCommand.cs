using Draya.Application.Classrooms.DTOs;
using MediatR;

namespace Draya.Application.Classrooms.Commands.CreateClassroom;

public record CreateClassroomCommand(
    Guid TeacherId,
    Guid SubjectId,
    string Name,
    Guid ClassroomTypeId,
    Guid GradeLevelId,
    DateTime StartDate,
    DateTime EndDate,
    decimal Price
) : IRequest<ClassroomDto>;
