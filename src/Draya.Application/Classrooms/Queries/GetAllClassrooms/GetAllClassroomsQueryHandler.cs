using Draya.Application.Classrooms.DTOs;
using Draya.Domain.Classrooms;
using MediatR;
using System.Linq;

namespace Draya.Application.Classrooms.Queries.GetAllClassrooms;

public class GetAllClassroomsQueryHandler : IRequestHandler<GetAllClassroomsQuery, PagedResult<ClassroomDto>>
{
    private readonly IClassroomRepository _classroomRepository;

    public GetAllClassroomsQueryHandler(IClassroomRepository classroomRepository)
    {
        _classroomRepository = classroomRepository;
    }

    public async Task<PagedResult<ClassroomDto>> Handle(GetAllClassroomsQuery request, CancellationToken cancellationToken)
    {
        var queryable = _classroomRepository.GetQueryable();
        
        // Only return active classrooms for the general search
        queryable = queryable.Where(c => c.IsActive);

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
