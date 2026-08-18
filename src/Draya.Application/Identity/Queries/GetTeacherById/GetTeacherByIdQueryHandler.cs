using Draya.Application.Identity.DTOs;
using Draya.Domain.Identity;
using MediatR;
using System.ComponentModel.DataAnnotations;

using Draya.Domain.Classrooms;
using Draya.Domain.Materials;

namespace Draya.Application.Identity.Queries.GetTeacherById;

public class GetTeacherByIdQueryHandler : IRequestHandler<GetTeacherByIdQuery, TeacherProfileDto>
{
    private readonly ITeacherRepository _teacherRepository;
    private readonly IClassroomRepository _classroomRepository;
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly IMaterialRepository _materialRepository;

    public GetTeacherByIdQueryHandler(
        ITeacherRepository teacherRepository,
        IClassroomRepository classroomRepository,
        IEnrollmentRepository enrollmentRepository,
        IMaterialRepository materialRepository)
    {
        _teacherRepository = teacherRepository;
        _classroomRepository = classroomRepository;
        _enrollmentRepository = enrollmentRepository;
        _materialRepository = materialRepository;
    }

    public async Task<TeacherProfileDto> Handle(GetTeacherByIdQuery request, CancellationToken cancellationToken)
    {
        var teacher = await _teacherRepository.GetByUserIdAsync(request.TeacherId, cancellationToken);

        if (teacher == null)
        {
            throw new ValidationException("Teacher not found."); // Should ideally use a NotFoundException
        }

        var classroomsCount = await _classroomRepository.GetCountByTeacherIdAsync(teacher.UserId, cancellationToken);
        var studentsCount = await _enrollmentRepository.GetActiveEnrollmentCountByTeacherAsync(teacher.UserId, cancellationToken);
        var lessonsCount = await _materialRepository.GetCountByTeacherIdAsync(teacher.UserId, cancellationToken);

        return new TeacherProfileDto(
            teacher.UserId,
            string.Empty,
            teacher.FullName,
            teacher.Phone,
            teacher.Specialization,
            teacher.Description,
            teacher.ProfilePictureUrl,
            classroomsCount,
            studentsCount,
            lessonsCount,
            null
        );
    }
}

