using Draya.Application.Classrooms.DTOs;
using Draya.Domain.Classrooms;
using Draya.Domain.Classrooms.Exceptions;
using MediatR;

namespace Draya.Application.Classrooms.Commands.EnrollStudent;

public class EnrollStudentCommandHandler : IRequestHandler<EnrollStudentCommand, EnrollmentResultDto>
{
    private readonly IClassroomRepository _classroomRepository;
    private readonly IEnrollmentRepository _enrollmentRepository;

    public EnrollStudentCommandHandler(
        IClassroomRepository classroomRepository,
        IEnrollmentRepository enrollmentRepository)
    {
        _classroomRepository = classroomRepository;
        _enrollmentRepository = enrollmentRepository;
    }

    public async Task<EnrollmentResultDto> Handle(EnrollStudentCommand request, CancellationToken cancellationToken)
    {
        var classroom = await _classroomRepository.GetByEnrollmentCodeAsync(
            request.EnrollmentCode,
            cancellationToken);

        if (classroom == null)
        {
            throw new EnrollmentCodeInvalidException();
        }

        if (!classroom.IsActive)
        {
            throw new ClassroomInactiveException();
        }

        var existingEnrollment = await _enrollmentRepository.GetByStudentAndClassroomAsync(
            request.StudentId,
            classroom.Id,
            cancellationToken);

        bool isReEnrollment;
        Enrollment enrollment;

        if (existingEnrollment != null && existingEnrollment.Status == EnrollmentStatus.Active)
        {
            throw new AlreadyEnrolledException();
        }
        else if (existingEnrollment != null && (existingEnrollment.Status == EnrollmentStatus.Unenrolled || existingEnrollment.Status == EnrollmentStatus.Revoked))
        {
            // Reactivate the removed enrollment instead of inserting a duplicate
            existingEnrollment.Status = EnrollmentStatus.Active;
            existingEnrollment.EnrolledAt = DateTime.UtcNow;
            await _enrollmentRepository.SaveChangesAsync(cancellationToken);
            enrollment = existingEnrollment;
            isReEnrollment = true;
        }
        else
        {
            // Fresh enrollment
            enrollment = new Enrollment
            {
                StudentId = request.StudentId,
                ClassroomId = classroom.Id,
                Status = EnrollmentStatus.Active
            };

            await _enrollmentRepository.AddAsync(enrollment, cancellationToken);
            await _enrollmentRepository.SaveChangesAsync(cancellationToken);
            isReEnrollment = false;
        }

        // Single-use enrollment code requirement: Immediately regenerate code so it cannot be reused by another student
        classroom.EnrollmentCode = GenerateEnrollmentCode();
        await _classroomRepository.SaveChangesAsync(cancellationToken);

        return new EnrollmentResultDto(classroom.Id, enrollment.EnrolledAt, isReEnrollment);
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
