using Draya.Application.Classrooms.DTOs;
using Draya.Domain.Classrooms;
using MediatR;
using System.Linq;

namespace Draya.Application.Classrooms.Queries.GetTeacherClassrooms;

public class GetTeacherClassroomsQueryHandler : IRequestHandler<GetTeacherClassroomsQuery, PagedResult<ClassroomDto>>
{
    private readonly IClassroomRepository _classroomRepository;
    private readonly Draya.Domain.Identity.ITeacherRepository _teacherRepository;
    private readonly Draya.Domain.Materials.IMaterialRepository _materialRepository;
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly ISectionRepository _sectionRepository;

    public GetTeacherClassroomsQueryHandler(
        IClassroomRepository classroomRepository,
        Draya.Domain.Identity.ITeacherRepository teacherRepository,
        Draya.Domain.Materials.IMaterialRepository materialRepository,
        IEnrollmentRepository enrollmentRepository,
        ISectionRepository sectionRepository)
    {
        _classroomRepository = classroomRepository;
        _teacherRepository = teacherRepository;
        _materialRepository = materialRepository;
        _enrollmentRepository = enrollmentRepository;
        _sectionRepository = sectionRepository;
    }

    public async Task<PagedResult<ClassroomDto>> Handle(GetTeacherClassroomsQuery request, CancellationToken cancellationToken)
    {
        var queryable = _classroomRepository.GetQueryable();
        
        queryable = queryable.Where(c => c.TeacherId == request.TeacherId);

        if (request.SubjectId.HasValue)
            queryable = queryable.Where(c => c.SubjectId == request.SubjectId.Value);
            
        if (request.GradeLevelId.HasValue)
            queryable = queryable.Where(c => c.GradeLevelId == request.GradeLevelId.Value);
            
        if (request.ClassroomTypeId.HasValue)
            queryable = queryable.Where(c => c.ClassroomTypeId == request.ClassroomTypeId.Value);

        var totalCount = queryable.Count();
        var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);

        var classrooms = queryable
            .OrderByDescending(c => c.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        var items = new List<ClassroomDto>();
        foreach (var c in classrooms)
        {
            var teacher = await _teacherRepository.GetByUserIdAsync(c.TeacherId, cancellationToken);
            var materialsCount = await _materialRepository.GetCountByClassroomIdAsync(c.Id, cancellationToken);
            var sectionsCount = _sectionRepository.GetQueryable().Count(s => s.ClassroomId == c.Id);
            var studentCount = await _enrollmentRepository.GetCountByClassroomIdAsync(c.Id, cancellationToken);

            items.Add(new ClassroomDto(
                c.Id,
                c.TeacherId,
                c.Subject?.Name ?? string.Empty,
                c.Name,
                c.EnrollmentCode,
                c.IsActive,
                studentCount,
                c.CreatedAt,
                c.ClassroomType?.Name ?? string.Empty,
                c.GradeLevel?.Name ?? string.Empty,
                c.StartDate,
                c.EndDate,
                c.Price,
                c.ImageUrl,
                materialsCount,
                sectionsCount, // SectionsCount
                materialsCount, // LessonsCount
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
