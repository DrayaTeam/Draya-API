using Draya.Application.Classrooms.DTOs;
using Draya.Domain.Classrooms;
using MediatR;
using System.Linq;

using Draya.Domain.Materials;

namespace Draya.Application.Classrooms.Queries.GetStudentClassrooms;

public class GetStudentClassroomsQueryHandler : IRequestHandler<GetStudentClassroomsQuery, PagedResult<ClassroomDto>>
{
    private readonly IClassroomRepository _classroomRepository;
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly IMaterialRepository _materialRepository;
    private readonly Draya.Domain.Identity.ITeacherRepository _teacherRepository;
    private readonly ISectionRepository _sectionRepository;

    public GetStudentClassroomsQueryHandler(
        IClassroomRepository classroomRepository,
        IEnrollmentRepository enrollmentRepository,
        IMaterialRepository materialRepository,
        Draya.Domain.Identity.ITeacherRepository teacherRepository,
        ISectionRepository sectionRepository)
    {
        _classroomRepository = classroomRepository;
        _enrollmentRepository = enrollmentRepository;
        _materialRepository = materialRepository;
        _teacherRepository = teacherRepository;
        _sectionRepository = sectionRepository;
    }

    public async Task<PagedResult<ClassroomDto>> Handle(GetStudentClassroomsQuery request, CancellationToken cancellationToken)
    {
        // First get all enrolled classroom IDs for this student
        var enrolledClassroomIds = await _classroomRepository.GetEnrolledClassroomIdsAsync(request.StudentId, cancellationToken);
        
        // Use a generic search method or query to filter
        // We might need to implement SearchClassroomsAsync in IClassroomRepository 
        // to handle the filters + a specific list of IDs.
        
        var queryable = _classroomRepository.GetQueryable();
        
        queryable = queryable.Where(c => enrolledClassroomIds.Contains(c.Id));

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
            var studentEnrollment = await _enrollmentRepository.GetByStudentAndClassroomAsync(request.StudentId, c.Id, cancellationToken);
            var materialsCount = await _materialRepository.GetCountByClassroomIdAsync(c.Id, cancellationToken);
            var sectionsCount = _sectionRepository.GetQueryable().Count(s => s.ClassroomId == c.Id);
            
            StudentProgressDto? studentProgress = null;
            if (studentEnrollment != null && studentEnrollment.Status == EnrollmentStatus.Active)
            {
                var materialsResult = await _materialRepository.GetByClassroomIdAsync(c.Id, 1, 1, cancellationToken);
                var totalLessons = materialsResult.TotalCount;
                var progressPercent = totalLessons == 0 ? 0 : (int)Math.Round((double)studentEnrollment.CompletedLessons / totalLessons * 100);

                studentProgress = new StudentProgressDto(
                    studentEnrollment.CompletedLessons,
                    totalLessons,
                    progressPercent,
                    studentEnrollment.LastAccessedAt
                );
            }



            items.Add(new ClassroomDto(
                c.Id,
                c.TeacherId,
                c.Subject?.Name ?? string.Empty,
                c.Name,
                string.Empty, // Hide enrollment code from normal list view
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
                sectionsCount, // SectionsCount
                materialsCount, // LessonsCount
                studentProgress,
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
