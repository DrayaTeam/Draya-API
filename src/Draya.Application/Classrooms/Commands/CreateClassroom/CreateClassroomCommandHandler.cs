using Draya.Application.Classrooms.DTOs;
using Draya.Domain.Classrooms;
using Draya.Domain.Classrooms.Exceptions;
using MediatR;

namespace Draya.Application.Classrooms.Commands.CreateClassroom;

public class CreateClassroomCommandHandler : IRequestHandler<CreateClassroomCommand, ClassroomDto>
{
    private readonly IClassroomRepository _classroomRepository;
    private readonly ISubjectRepository _subjectRepository;

    public CreateClassroomCommandHandler(
        IClassroomRepository classroomRepository,
        ISubjectRepository subjectRepository)
    {
        _classroomRepository = classroomRepository;
        _subjectRepository = subjectRepository;
    }

    public async Task<ClassroomDto> Handle(CreateClassroomCommand request, CancellationToken cancellationToken)
    {
        var subject = await _subjectRepository.GetByIdAsync(request.SubjectId, cancellationToken);
        if (subject == null)
        {
            throw new SubjectNotFoundException(request.SubjectId);
        }

        var classroom = new Classroom
        {
            TeacherId = request.TeacherId,
            SubjectId = request.SubjectId,
            Name = request.Name.Trim(),
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
            classroom.CreatedAt
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
