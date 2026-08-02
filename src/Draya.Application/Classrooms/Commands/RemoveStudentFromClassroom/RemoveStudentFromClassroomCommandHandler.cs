using Draya.Domain.Classrooms;
using Draya.Domain.Classrooms.Exceptions;
using MediatR;

namespace Draya.Application.Classrooms.Commands.RemoveStudentFromClassroom;

public class RemoveStudentFromClassroomCommandHandler : IRequestHandler<RemoveStudentFromClassroomCommand>
{
    private readonly IClassroomRepository _classroomRepository;
    private readonly IEnrollmentRepository _enrollmentRepository;

    public RemoveStudentFromClassroomCommandHandler(
        IClassroomRepository classroomRepository,
        IEnrollmentRepository enrollmentRepository)
    {
        _classroomRepository = classroomRepository;
        _enrollmentRepository = enrollmentRepository;
    }

    public async Task Handle(RemoveStudentFromClassroomCommand request, CancellationToken cancellationToken)
    {
        var classroom = await _classroomRepository.GetByIdAsync(request.ClassroomId, cancellationToken);
        
        if (classroom == null || classroom.TeacherId != request.TeacherId)
        {
            throw new ClassroomNotFoundException();
        }

        var enrollment = await _enrollmentRepository.GetByStudentAndClassroomAsync(
            request.StudentId,
            request.ClassroomId,
            cancellationToken);

        if (enrollment == null || enrollment.Status != EnrollmentStatus.Active)
        {
            throw new StudentNotEnrolledException();
        }

        enrollment.Status = EnrollmentStatus.Removed;

        await _enrollmentRepository.SaveChangesAsync(cancellationToken);
    }
}
