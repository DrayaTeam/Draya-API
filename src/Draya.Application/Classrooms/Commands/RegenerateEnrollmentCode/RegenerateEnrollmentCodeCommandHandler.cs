using Draya.Application.Classrooms.DTOs;
using Draya.Domain.Classrooms;
using Draya.Domain.Classrooms.Exceptions;
using MediatR;

namespace Draya.Application.Classrooms.Commands.RegenerateEnrollmentCode;

public class RegenerateEnrollmentCodeCommandHandler : IRequestHandler<RegenerateEnrollmentCodeCommand, ClassroomDto>
{
    private readonly IClassroomRepository _classroomRepository;

    public RegenerateEnrollmentCodeCommandHandler(IClassroomRepository classroomRepository)
    {
        _classroomRepository = classroomRepository;
    }

    public async Task<ClassroomDto> Handle(RegenerateEnrollmentCodeCommand request, CancellationToken cancellationToken)
    {
        var classroom = await _classroomRepository.GetByIdAsync(request.ClassroomId, cancellationToken);
        
        if (classroom == null || classroom.TeacherId != request.TeacherId)
        {
            throw new ClassroomNotFoundException();
        }

        classroom.EnrollmentCode = GenerateEnrollmentCode();

        await _classroomRepository.SaveChangesAsync(cancellationToken);

        return new ClassroomDto(
            classroom.Id,
            classroom.TeacherId,
            classroom.Subject?.Name ?? string.Empty,
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
