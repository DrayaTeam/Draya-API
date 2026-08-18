using Draya.Application.Classrooms.DTOs;
using Draya.Domain.Classrooms;
using MediatR;
using System.Linq;

namespace Draya.Application.Classrooms.Queries.GetStudentClassrooms;

public class GetStudentClassroomsQueryHandler : IRequestHandler<GetStudentClassroomsQuery, PagedResult<ClassroomDto>>
{
    private readonly IClassroomRepository _classroomRepository;

    public GetStudentClassroomsQueryHandler(IClassroomRepository classroomRepository)
    {
        _classroomRepository = classroomRepository;
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

        var items = classrooms.Select(c => new ClassroomDto(
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
            c.ImageUrl
        )).ToList();

        return new PagedResult<ClassroomDto>(
            items,
            request.Page,
            request.PageSize,
            totalCount,
            totalPages
        );
    }
}
