using Draya.Application.Common.Interfaces;
using Draya.Domain.Classrooms;
using MediatR;

namespace Draya.Application.Classrooms.Commands.DeleteSection;

public class DeleteSectionCommandHandler : IRequestHandler<DeleteSectionCommand>
{
    private readonly ISectionRepository _sectionRepository;
    private readonly IClassroomRepository _classroomRepository;

    public DeleteSectionCommandHandler(
        ISectionRepository sectionRepository,
        IClassroomRepository classroomRepository)
    {
        _sectionRepository = sectionRepository;
        _classroomRepository = classroomRepository;
    }

    public async Task Handle(DeleteSectionCommand request, CancellationToken cancellationToken)
    {
        var section = await _sectionRepository.GetByIdAsync(request.SectionId, cancellationToken);
        if (section == null)
            throw new Exception($"ClassroomSection {request.SectionId} not found.");

        var classroom = await _classroomRepository.GetByIdAsync(section.ClassroomId, cancellationToken);
        if (classroom == null)
            throw new Exception($"Classroom {section.ClassroomId} not found.");

        if (classroom.TeacherId != request.TeacherId)
        {
            throw new UnauthorizedAccessException();
        }

        _sectionRepository.Remove(section);
        await _sectionRepository.SaveChangesAsync(cancellationToken);
    }
}
