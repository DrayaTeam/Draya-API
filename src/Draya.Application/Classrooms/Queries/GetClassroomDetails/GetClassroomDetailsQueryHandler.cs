using Draya.Application.Classrooms.DTOs;
using Draya.Domain.Classrooms;
using Draya.Domain.Classrooms.Exceptions;
using Draya.Domain.Materials;
using MediatR;

namespace Draya.Application.Classrooms.Queries.GetClassroomDetails;

public class GetClassroomDetailsQueryHandler : IRequestHandler<GetClassroomDetailsQuery, ClassroomDto>
{
    private readonly IClassroomRepository _classroomRepository;
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly IMaterialRepository _materialRepository;
    private readonly Draya.Domain.Identity.ITeacherRepository _teacherRepository;

    public GetClassroomDetailsQueryHandler(
        IClassroomRepository classroomRepository,
        IEnrollmentRepository enrollmentRepository,
        IMaterialRepository materialRepository,
        Draya.Domain.Identity.ITeacherRepository teacherRepository)
    {
        _classroomRepository = classroomRepository;
        _enrollmentRepository = enrollmentRepository;
        _materialRepository = materialRepository;
        _teacherRepository = teacherRepository;
    }

    public async Task<ClassroomDto> Handle(GetClassroomDetailsQuery request, CancellationToken cancellationToken)
    {
        var classroom = await _classroomRepository.GetByIdAsync(request.ClassroomId, cancellationToken);
        
        if (classroom == null)
        {
            throw new ClassroomNotFoundException();
        }

        var isAuthorized = false;
        Enrollment? studentEnrollment = null;

        if (request.UserRole == "Teacher")
        {
            isAuthorized = classroom.TeacherId == request.UserId;
        }
        else if (request.UserRole == "Student")
        {
            studentEnrollment = await _enrollmentRepository.GetByStudentAndClassroomAsync(request.UserId, request.ClassroomId, cancellationToken);
            isAuthorized = studentEnrollment != null && studentEnrollment.Status == EnrollmentStatus.Active;
        }

        string returnedEnrollmentCode = classroom.EnrollmentCode;

        if (!isAuthorized)
        {
            if (request.UserRole == "Student" && classroom.IsActive)
            {
                // Unenrolled student viewing public details
                returnedEnrollmentCode = string.Empty;
            }
            else
            {
                throw new ClassroomNotFoundException();
            }
        }

        var materialsCount = await _materialRepository.GetCountByClassroomIdAsync(request.ClassroomId, cancellationToken);

        StudentProgressDto? studentProgress = null;
        if (isAuthorized && request.UserRole == "Student" && studentEnrollment != null)
        {
            var totalLessons = materialsCount;
            var progressPercent = totalLessons == 0 ? 0 : (int)Math.Round((double)studentEnrollment.CompletedLessons / totalLessons * 100);

            studentProgress = new StudentProgressDto(
                studentEnrollment.CompletedLessons,
                totalLessons,
                progressPercent,
                studentEnrollment.LastAccessedAt
            );
        }

        var teacher = await _teacherRepository.GetByUserIdAsync(classroom.TeacherId, cancellationToken);

        return new ClassroomDto(
            classroom.Id,
            classroom.TeacherId,
            classroom.Subject?.Name ?? string.Empty,
            classroom.Name,
            returnedEnrollmentCode,
            classroom.IsActive,
            0,
            classroom.CreatedAt,
            classroom.ClassroomType?.Name ?? string.Empty,
            classroom.GradeLevel?.Name ?? string.Empty,
            classroom.StartDate,
            classroom.EndDate,
            classroom.Price,
            classroom.ImageUrl,
            materialsCount,
            studentProgress,
            teacher?.FullName,
            teacher?.ProfilePictureUrl
        );
    }
}
