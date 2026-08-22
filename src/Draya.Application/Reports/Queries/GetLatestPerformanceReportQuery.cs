using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Draya.Domain.Reports;
using MediatR;

namespace Draya.Application.Reports.Queries;

public record PerformanceReportDto(
    Guid Id,
    DateTime GeneratedAt,
    string SummaryText,
    List<WeakTopicDto> WeakTopics,
    List<SubjectProficiencyDto> SubjectProficiencies,
    int TotalQuestionsAsked,
    int TotalQuestionsReplied,
    decimal AverageExamDurationMinutes,
    int CompletedLessons,
    decimal ClassroomPercentile
);

public record WeakTopicDto(
    string TopicName,
    decimal ProficiencyPercent,
    string Recommendation
);

public record SubjectProficiencyDto(
    string SubjectName,
    decimal ProficiencyPercent
);

public record GetLatestPerformanceReportQuery(Guid StudentId) : IRequest<PerformanceReportDto?>;

public class GetLatestPerformanceReportQueryHandler : IRequestHandler<GetLatestPerformanceReportQuery, PerformanceReportDto?>
{
    private readonly IPerformanceReportRepository _repo;

    public GetLatestPerformanceReportQueryHandler(IPerformanceReportRepository repo)
    {
        _repo = repo;
    }

    public async Task<PerformanceReportDto?> Handle(GetLatestPerformanceReportQuery request, CancellationToken cancellationToken)
    {
        var report = await _repo.GetLatestByStudentIdAsync(request.StudentId, cancellationToken);

        if (report == null)
            return null;

        var weakTopics = report.WeakTopics.Select(w => new WeakTopicDto(
            w.TopicName,
            w.ProficiencyPercent,
            w.Recommendation
        )).ToList();

        var proficiencies = report.SubjectProficiencies.Select(p => new SubjectProficiencyDto(
            p.SubjectName,
            p.ProficiencyPercent
        )).ToList();

        return new PerformanceReportDto(
            report.Id,
            report.GeneratedAt,
            report.SummaryText,
            weakTopics,
            proficiencies,
            report.TotalQuestionsAsked,
            report.TotalQuestionsReplied,
            report.AverageExamDurationMinutes,
            report.CompletedLessons,
            report.ClassroomPercentile
        );
    }
}
