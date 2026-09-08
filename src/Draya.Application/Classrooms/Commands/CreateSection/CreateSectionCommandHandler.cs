using Draya.Application.Common.Interfaces;
using Draya.Domain.Classrooms;
using MediatR;

namespace Draya.Application.Classrooms.Commands.CreateSection;

public class CreateSectionCommandHandler : IRequestHandler<CreateSectionCommand, Guid>
{
    private readonly ISectionRepository _sectionRepository;
    private readonly IClassroomRepository _classroomRepository;

    public CreateSectionCommandHandler(
        ISectionRepository sectionRepository,
        IClassroomRepository classroomRepository)
    {
        _sectionRepository = sectionRepository;
        _classroomRepository = classroomRepository;
    }

    public async Task<Guid> Handle(CreateSectionCommand request, CancellationToken cancellationToken)
    {
        var classroom = await _classroomRepository.GetByIdAsync(request.ClassroomId, cancellationToken);
        if (classroom == null)
            throw new Exception($"Classroom {request.ClassroomId} not found.");

        if (classroom.TeacherId != request.TeacherId)
        {
            throw new UnauthorizedAccessException();
        }

        var section = new ClassroomSection
        {
            ClassroomId = request.ClassroomId,
            Title = request.Title,
            Description = request.Description,
            Order = request.Order,
            CreatedAt = DateTime.UtcNow
        };

        await _sectionRepository.AddAsync(section, cancellationToken);
        await _sectionRepository.SaveChangesAsync(cancellationToken);
        
        return section.Id;
    }
}
