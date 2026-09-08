using Draya.Application.Classrooms.DTOs;
using MediatR;

namespace Draya.Application.Classrooms.Commands.UpdateClassroom;

public record UpdateClassroomCommand(
    Guid ClassroomId,
    Guid TeacherId,
    string Name,
    Guid SubjectId,
    Guid ClassroomTypeId,
    Guid GradeLevelId,
    DateTime StartDate,
    DateTime EndDate,
    decimal Price,
    bool IsActive,
    string? ImageUrl = null
) : IRequest<ClassroomDto>;
