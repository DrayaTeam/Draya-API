using MediatR;

namespace Draya.Application.Classrooms.Commands.DeleteSection;

public record DeleteSectionCommand(Guid SectionId, Guid TeacherId) : IRequest;
