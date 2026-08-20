using Draya.Application.Classrooms.DTOs;
using Draya.Application.Exams.DTOs;
using Draya.Domain.Classrooms;
using Draya.Domain.Exams;
using MediatR;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Draya.Application.Exams.Queries.GetStudentExams;

public class GetStudentExamsQueryHandler : IRequestHandler<GetStudentExamsQuery, PagedResult<StudentExamSummaryDto>>
{
    private readonly IExamRepository _examRepository;
    private readonly IClassroomRepository _classroomRepository;

    public GetStudentExamsQueryHandler(IExamRepository examRepository, IClassroomRepository classroomRepository)
    {
        _examRepository = examRepository;
        _classroomRepository = classroomRepository;
    }

    public async Task<PagedResult<StudentExamSummaryDto>> Handle(GetStudentExamsQuery request, CancellationToken cancellationToken)
    {
        var classroomIds = await _classroomRepository.GetEnrolledClassroomIdsAsync(request.StudentId, cancellationToken);
        var queryable = _examRepository.GetQueryable()
            .Where(e => classroomIds.Contains(e.ClassroomId));

        var totalCount = queryable.Count();
        var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);

        var exams = queryable
            .OrderByDescending(e => e.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        var items = exams.Select(e => new StudentExamSummaryDto(
            e.Id,
            e.ClassroomId,
            e.SectionId,
            e.Title,
            e.Topic,
            e.DurationMinutes,
            e.StartDate,
            e.EndDate,
            e.AllowedAttempts,
            e.CreatedAt
        )).ToList();

        return new PagedResult<StudentExamSummaryDto>(
            items,
            request.Page,
            request.PageSize,
            totalCount,
            totalPages
        );
    }
}
