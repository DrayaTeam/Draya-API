using Draya.Application.Common.Interfaces;
using Draya.Domain.Classrooms;
using MediatR;

namespace Draya.Application.Classrooms.Commands.UpdateSection;

public class UpdateSectionCommandHandler : IRequestHandler<UpdateSectionCommand>
{
    private readonly ISectionRepository _sectionRepository;
    private readonly IClassroomRepository _classroomRepository;

    public UpdateSectionCommandHandler(
        ISectionRepository sectionRepository,
        IClassroomRepository classroomRepository)
    {
        _sectionRepository = sectionRepository;
        _classroomRepository = classroomRepository;
    }

    public async Task Handle(UpdateSectionCommand request, CancellationToken cancellationToken)
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

        section.Title = request.Title;
        section.Description = request.Description;
        section.Order = request.Order;

        _sectionRepository.Update(section);
        await _sectionRepository.SaveChangesAsync(cancellationToken);
    }
}
