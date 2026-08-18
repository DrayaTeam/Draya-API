using MediatR;

namespace Draya.Application.Classrooms.Commands.CreateSection;

public record CreateSectionCommand(
    Guid ClassroomId,
    Guid TeacherId,
    string Title,
    string Description,
    int Order) : IRequest<Guid>;
