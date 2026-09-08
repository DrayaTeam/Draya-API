using Draya.Application.Classrooms.DTOs;
using Draya.Domain.Classrooms;
using Draya.Domain.Classrooms.Exceptions;
using MediatR;

namespace Draya.Application.Classrooms.Commands.CreateClassroom;

    public class CreateClassroomCommandHandler : IRequestHandler<CreateClassroomCommand, ClassroomDto>
{
    private readonly IClassroomRepository _classroomRepository;
    private readonly ISubjectRepository _subjectRepository;
    private readonly IClassroomTypeRepository _classroomTypeRepository;
    private readonly IGradeLevelRepository _gradeLevelRepository;

    public CreateClassroomCommandHandler(
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

    public async Task<ClassroomDto> Handle(CreateClassroomCommand request, CancellationToken cancellationToken)
    {
        var subject = await _subjectRepository.GetByIdAsync(request.SubjectId, cancellationToken);
        if (subject == null)
        {
            throw new SubjectNotFoundException(request.SubjectId);
        }

        var classroomType = await _classroomTypeRepository.GetByIdAsync(request.ClassroomTypeId, cancellationToken);
        if (classroomType == null || !classroomType.IsActive)
        {
            throw new Exception("ClassroomType not found or inactive.");
        }

        var gradeLevel = await _gradeLevelRepository.GetByIdAsync(request.GradeLevelId, cancellationToken);
        if (gradeLevel == null || !gradeLevel.IsActive)
        {
            throw new Exception("GradeLevel not found or inactive.");
        }

        var classroom = new Classroom
        {
            TeacherId = request.TeacherId,
            SubjectId = request.SubjectId,
            ClassroomTypeId = request.ClassroomTypeId,
            GradeLevelId = request.GradeLevelId,
            Name = request.Name.Trim(),
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Price = request.Price,
            ImageUrl = request.ImageUrl?.Trim(),
            EnrollmentCode = GenerateEnrollmentCode(),
            IsActive = true
        };

        await _classroomRepository.AddAsync(classroom, cancellationToken);
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
            classroom.Price,
            classroom.ImageUrl
        );
    }

    private static string GenerateEnrollmentCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var random = new Random();
        var code = new char[8];
        
        for (int i = 0; i < 4; i++)
        {
            code[i] = chars[random.Next(chars.Length)];
        }
        
        code[4] = '-';
        
        for (int i = 5; i < 8; i++)
        {
            code[i] = chars[random.Next(chars.Length)];
        }
        
        return new string(code);
    }
}
