using Draya.Application.Classrooms.DTOs;
using Draya.Domain.Classrooms;
using MediatR;

namespace Draya.Application.Classrooms.Queries.GetTeacherClassrooms;

public class GetTeacherClassroomsQueryHandler : IRequestHandler<GetTeacherClassroomsQuery, PagedResult<ClassroomDto>>
{
    private readonly IClassroomRepository _classroomRepository;

    public GetTeacherClassroomsQueryHandler(IClassroomRepository classroomRepository)
    {
        _classroomRepository = classroomRepository;
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

        var items = classrooms.Select(c => new ClassroomDto(
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
