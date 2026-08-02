using Draya.Application.Classrooms.DTOs;
using Draya.Domain.Classrooms;
using Draya.Domain.Classrooms.Exceptions;
using MediatR;

namespace Draya.Application.Classrooms.Commands.UpdateClassroom;

public class UpdateClassroomCommandHandler : IRequestHandler<UpdateClassroomCommand, ClassroomDto>
{
    private readonly IClassroomRepository _classroomRepository;
    private readonly ISubjectRepository _subjectRepository;

    public UpdateClassroomCommandHandler(
        IClassroomRepository classroomRepository,
        ISubjectRepository subjectRepository)
    {
        _classroomRepository = classroomRepository;
        _subjectRepository = subjectRepository;
    }

    public async Task<ClassroomDto> Handle(UpdateClassroomCommand request, CancellationToken cancellationToken)
    {
        var classroom = await _classroomRepository.GetByIdAsync(request.ClassroomId, cancellationToken);
        
        if (classroom == null || classroom.TeacherId != request.TeacherId)
        {
            throw new ClassroomNotFoundException();
        }

        var subject = await _subjectRepository.GetByIdAsync(request.SubjectId, cancellationToken);
        if (subject == null)
        {
            throw new SubjectNotFoundException(request.SubjectId);
        }

        classroom.Name = request.Name.Trim();
        classroom.SubjectId = request.SubjectId;
        classroom.IsActive = request.IsActive;

        await _classroomRepository.SaveChangesAsync(cancellationToken);

        return new ClassroomDto(
            classroom.Id,
            classroom.TeacherId,
            subject.Name,
            classroom.Name,
            classroom.EnrollmentCode,
            classroom.IsActive,
            0,
            classroom.CreatedAt
        );
    }
}
