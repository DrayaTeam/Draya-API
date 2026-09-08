using MediatR;

namespace Draya.Application.Classrooms.Commands.UpdateSection;

public record UpdateSectionCommand(
    Guid SectionId,
    Guid TeacherId,
    string Title,
    string Description,
    int Order) : IRequest;
