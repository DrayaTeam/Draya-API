using Draya.Application.Classrooms.DTOs;
using Draya.Domain.Classrooms;
using MediatR;
using System.Linq;

namespace Draya.Application.Classrooms.Queries.GetClassroomsByTeacher;

public class GetClassroomsByTeacherQueryHandler : IRequestHandler<GetClassroomsByTeacherQuery, PagedResult<ClassroomDto>>
{
    private readonly IClassroomRepository _classroomRepository;

    public GetClassroomsByTeacherQueryHandler(IClassroomRepository classroomRepository)
    {
        _classroomRepository = classroomRepository;
    }

    public async Task<PagedResult<ClassroomDto>> Handle(GetClassroomsByTeacherQuery request, CancellationToken cancellationToken)
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
            c.Price
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
