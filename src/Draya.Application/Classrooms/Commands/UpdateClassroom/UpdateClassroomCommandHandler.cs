using Draya.Application.Classrooms.DTOs;
using Draya.Domain.Classrooms;
using Draya.Domain.Classrooms.Exceptions;
using MediatR;

namespace Draya.Application.Classrooms.Commands.UpdateClassroom;

public class UpdateClassroomCommandHandler : IRequestHandler<UpdateClassroomCommand, ClassroomDto>
{
    private readonly IClassroomRepository _classroomRepository;
    private readonly ISubjectRepository _subjectRepository;
    private readonly IClassroomTypeRepository _classroomTypeRepository;
    private readonly IGradeLevelRepository _gradeLevelRepository;

    public UpdateClassroomCommandHandler(
        IClassroomRepository classroomRepository,
        ISubjectRepository subjectRepository,
        IClassroomTypeRepository classroomTypeRepository,
        IGradeLevelRepository gradeLevelRepository)
    {
        _classroomRepository = classroomRepository;
        _subjectRepository = subjectRepository;
        _classroomTypeRepository = classroomTypeRepository;
        _gradeLevelRepository = gradeLevelRepository;
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

        var classroomType = await _classroomTypeRepository.GetByIdAsync(request.ClassroomTypeId, cancellationToken);
        if (classroomType == null || (!classroomType.IsActive && classroomType.Id != classroom.ClassroomTypeId))
        {
            throw new Exception("ClassroomType not found or inactive.");
        }

        var gradeLevel = await _gradeLevelRepository.GetByIdAsync(request.GradeLevelId, cancellationToken);
        if (gradeLevel == null || (!gradeLevel.IsActive && gradeLevel.Id != classroom.GradeLevelId))
        {
            throw new Exception("GradeLevel not found or inactive.");
        }

        classroom.Name = request.Name.Trim();
        classroom.SubjectId = request.SubjectId;
        classroom.ClassroomTypeId = request.ClassroomTypeId;
        classroom.GradeLevelId = request.GradeLevelId;
        classroom.StartDate = request.StartDate;
        classroom.EndDate = request.EndDate;
        classroom.Price = request.Price;
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
            classroom.CreatedAt,
            classroomType.Name,
            gradeLevel.Name,
            classroom.StartDate,
            classroom.EndDate,
            classroom.Price
        );
    }
}
