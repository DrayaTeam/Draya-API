using Draya.Application.Identity.DTOs;
using Draya.Domain.Identity;
using MediatR;
using System.Linq;

using Draya.Domain.Classrooms;
using Draya.Domain.Materials;

namespace Draya.Application.Identity.Queries.GetTeachers;

public class GetTeachersQueryHandler : IRequestHandler<GetTeachersQuery, List<TeacherProfileDto>>
{
    private readonly ITeacherRepository _teacherRepository;
    private readonly IClassroomRepository _classroomRepository;
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly IMaterialRepository _materialRepository;

    public GetTeachersQueryHandler(
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

    public async Task<List<TeacherProfileDto>> Handle(GetTeachersQuery request, CancellationToken cancellationToken)
    {
        var teachers = await _teacherRepository.GetAllAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(request.Specialization))
        {
            teachers = teachers.Where(t => 
                t.Specialization != null && 
                t.Specialization.Contains(request.Specialization, StringComparison.OrdinalIgnoreCase));
        }

        var dtos = new List<TeacherProfileDto>();

        foreach (var t in teachers)
        {
            var classroomsCount = await _classroomRepository.GetCountByTeacherIdAsync(t.UserId, cancellationToken);
            var studentsCount = await _enrollmentRepository.GetActiveEnrollmentCountByTeacherAsync(t.UserId, cancellationToken);
            var lessonsCount = await _materialRepository.GetCountByTeacherIdAsync(t.UserId, cancellationToken);

            dtos.Add(new TeacherProfileDto(
                t.UserId,
                string.Empty, // Email is in AspNetUsers, we might need a different join or skip it for basic listing
                t.FullName,
                t.Phone,
                t.Specialization,
                t.Description,
                t.ProfilePictureUrl,
                classroomsCount,
                studentsCount,
                lessonsCount,
                null // AverageRating
            ));
        }

        return dtos;
    }
}

