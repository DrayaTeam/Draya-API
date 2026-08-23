using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Draya.Application.Reports.Services;

public record SubjectProficiencyResult(string SubjectName, decimal ProficiencyPercent);
public record TrendPointResult(DateTime Month, decimal AverageScore);
public record WeakTopicResult(string TopicName, string SubjectName, decimal ProficiencyPercent, string Status, List<string> ExampleIncorrectAnswers);

public record StudentAnalyticsDto(
    decimal OverallAverage,
    decimal HighestScore,
    int CompletedExams,
    List<SubjectProficiencyResult> SubjectProficiencies,
    List<TrendPointResult> TrendPoints,
    List<WeakTopicResult> WeakTopics,
    int TotalQuestionsAsked,
    int TotalQuestionsReplied,
    decimal AverageExamDurationMinutes,
    int CompletedLessons,
    decimal ClassroomPercentile
);

public interface IStudentAnalyticsService
{
    Task<StudentAnalyticsDto> GetAnalyticsAsync(Guid studentId, Guid? teacherId = null, CancellationToken cancellationToken = default);
}
