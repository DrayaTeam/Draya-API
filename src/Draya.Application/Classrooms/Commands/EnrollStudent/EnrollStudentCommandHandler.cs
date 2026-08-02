using Draya.Application.Classrooms.DTOs;
using Draya.Domain.Classrooms;
using Draya.Domain.Classrooms.Exceptions;
using Draya.Domain.Subscriptions;
using MediatR;

namespace Draya.Application.Classrooms.Commands.EnrollStudent;

public class EnrollStudentCommandHandler : IRequestHandler<EnrollStudentCommand, ClassroomDto>
{
    private readonly IClassroomRepository _classroomRepository;
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly ISubscriptionRepository _subscriptionRepository;

    public EnrollStudentCommandHandler(
        IClassroomRepository classroomRepository,
        IEnrollmentRepository enrollmentRepository,
        ISubscriptionRepository subscriptionRepository)
    {
        _classroomRepository = classroomRepository;
        _enrollmentRepository = enrollmentRepository;
        _subscriptionRepository = subscriptionRepository;
    }

    public async Task<ClassroomDto> Handle(EnrollStudentCommand request, CancellationToken cancellationToken)
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

        if (existingEnrollment != null && existingEnrollment.Status == EnrollmentStatus.Active)
        {
            throw new AlreadyEnrolledException();
        }

        var subscription = await _subscriptionRepository.GetActiveForTeacherAsync(
            classroom.TeacherId, 
            cancellationToken);

        if (subscription == null)
        {
            throw new QuotaExceededException("Teacher does not have an active subscription.");
        }

        var currentEnrollmentCount = await _enrollmentRepository.GetActiveEnrollmentCountByTeacherAsync(
            classroom.TeacherId,
            cancellationToken);

        if (currentEnrollmentCount >= subscription.Plan.MaxStudents)
        {
            throw new QuotaExceededException($"Classroom enrollment would exceed the teacher's subscription limit of {subscription.Plan.MaxStudents} students.");
        }

        var enrollment = new Enrollment
        {
            StudentId = request.StudentId,
            ClassroomId = classroom.Id,
            Status = EnrollmentStatus.Active
        };

        await _enrollmentRepository.AddAsync(enrollment, cancellationToken);
        await _enrollmentRepository.SaveChangesAsync(cancellationToken);

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
}
