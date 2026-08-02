using Draya.Domain.Classrooms;
using Draya.Domain.Classrooms.Exceptions;
using MediatR;

namespace Draya.Application.Classrooms.Commands.DeactivateClassroom;

public class DeactivateClassroomCommandHandler : IRequestHandler<DeactivateClassroomCommand>
{
    private readonly IClassroomRepository _classroomRepository;

    public DeactivateClassroomCommandHandler(IClassroomRepository classroomRepository)
    {
        _classroomRepository = classroomRepository;
    }

    public async Task Handle(DeactivateClassroomCommand request, CancellationToken cancellationToken)
    {
        var classroom = await _classroomRepository.GetByIdAsync(request.ClassroomId, cancellationToken);
        
        if (classroom == null || classroom.TeacherId != request.TeacherId)
        {
            throw new ClassroomNotFoundException();
        }

        classroom.IsActive = false;

        await _classroomRepository.SaveChangesAsync(cancellationToken);
    }
}
