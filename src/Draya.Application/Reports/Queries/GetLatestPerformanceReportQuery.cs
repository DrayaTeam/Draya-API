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
    string Recommendation,
    decimal PreviousProficiencyPercent,
    decimal Delta,
    bool IsResolved
);

public record SubjectProficiencyDto(
    string SubjectName,
    decimal ProficiencyPercent
);

public record GetLatestPerformanceReportQuery(Guid StudentId) : IRequest<PerformanceReportDto?>;

public class GetLatestPerformanceReportQueryHandler : IRequestHandler<GetLatestPerformanceReportQuery, PerformanceReportDto?>
{
    private readonly IPerformanceReportRepository _repo;
    private readonly IStudentWeaknessRepository _weaknessRepo;

    public GetLatestPerformanceReportQueryHandler(
        IPerformanceReportRepository repo,
        IStudentWeaknessRepository weaknessRepo)
    {
        _repo = repo;
        _weaknessRepo = weaknessRepo;
    }

    public async Task<PerformanceReportDto?> Handle(GetLatestPerformanceReportQuery request, CancellationToken cancellationToken)
    {
        var report = await _repo.GetLatestByStudentIdAsync(request.StudentId, cancellationToken);

        if (report == null)
            return null;

        var weakTopics = new List<WeakTopicDto>();
        
        foreach (var w in report.WeakTopics)
        {
            // Find the active/resolved status and history from the DB
            var dbWeakness = await _weaknessRepo.GetActiveByTopicAsync(request.StudentId, w.TopicName, cancellationToken);
                
            bool isResolved = false;
            decimal previous = w.ProficiencyPercent;
            decimal current = Math.Round(w.ProficiencyPercent, 2);
            
            if (dbWeakness != null)
            {
                isResolved = !dbWeakness.IsActive;
                current = Math.Round(dbWeakness.CurrentProficiencyPercent, 2);
                
                var histories = await _weaknessRepo.GetHistoryAsync(dbWeakness.Id, 2, cancellationToken);
                    
                if (histories.Count > 1) previous = histories[1].NewProficiencyPercent;
                else if (histories.Count == 1) previous = histories[0].PreviousProficiencyPercent;
            }

            weakTopics.Add(new WeakTopicDto(
                w.TopicName,
                current,
                w.Recommendation,
                Math.Round(previous, 2),
                Math.Round(current - previous, 2),
                isResolved
            ));
        }

        var proficiencies = report.SubjectProficiencies.Select(p => new SubjectProficiencyDto(
            p.SubjectName,
            Math.Round(p.ProficiencyPercent, 2)
        )).ToList();

        return new PerformanceReportDto(
            report.Id,
            report.GeneratedAt,
            report.SummaryText,
            weakTopics,
            proficiencies,
            report.TotalQuestionsAsked,
            report.TotalQuestionsReplied,
            Math.Round(report.AverageExamDurationMinutes, 2),
            report.CompletedLessons,
            Math.Round(report.ClassroomPercentile, 2)
        );
    }
}
