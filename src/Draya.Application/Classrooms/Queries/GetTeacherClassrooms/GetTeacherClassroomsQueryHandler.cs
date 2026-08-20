using Draya.Application.Classrooms.DTOs;
using Draya.Domain.Classrooms;
using MediatR;

namespace Draya.Application.Classrooms.Queries.GetTeacherClassrooms;

public class GetTeacherClassroomsQueryHandler : IRequestHandler<GetTeacherClassroomsQuery, PagedResult<ClassroomDto>>
{
    private readonly IClassroomRepository _classroomRepository;
    private readonly Draya.Domain.Identity.ITeacherRepository _teacherRepository;
    private readonly Draya.Domain.Materials.IMaterialRepository _materialRepository;

    public GetTeacherClassroomsQueryHandler(
        IClassroomRepository classroomRepository,
        Draya.Domain.Identity.ITeacherRepository teacherRepository,
        Draya.Domain.Materials.IMaterialRepository materialRepository)
    {
        _classroomRepository = classroomRepository;
        _teacherRepository = teacherRepository;
        _materialRepository = materialRepository;
    }

    public async Task<PagedResult<ClassroomDto>> Handle(GetTeacherClassroomsQuery request, CancellationToken cancellationToken)
    {
        var classrooms = await _classroomRepository.GetByTeacherIdAsync(
            request.TeacherId,
            request.Page,
            request.PageSize,
            cancellationToken);

        var totalCount = await _classroomRepository.GetCountByTeacherIdAsync(
            request.TeacherId,
            cancellationToken);

        var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);

        var items = new List<ClassroomDto>();
        foreach (var c in classrooms)
        {
            var teacher = await _teacherRepository.GetByUserIdAsync(c.TeacherId, cancellationToken);
            var materialsCount = await _materialRepository.GetCountByClassroomIdAsync(c.Id, cancellationToken);

            items.Add(new ClassroomDto(
                c.Id,
                c.TeacherId,
                c.Subject?.Name ?? string.Empty,
                c.Name,
                c.EnrollmentCode,
                c.IsActive,
                0,
                c.CreatedAt,
                c.ClassroomType?.Name ?? string.Empty,
                c.GradeLevel?.Name ?? string.Empty,
                c.StartDate,
                c.EndDate,
                c.Price,
                c.ImageUrl,
                materialsCount,
                null,
                teacher?.FullName,
                teacher?.ProfilePictureUrl
            ));
        }

        return new PagedResult<ClassroomDto>(
            items,
            request.Page,
            request.PageSize,
            totalCount,
            totalPages
        );
    }
}
