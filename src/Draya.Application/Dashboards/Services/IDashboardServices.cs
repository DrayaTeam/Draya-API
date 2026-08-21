using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Draya.Application.Dashboards.Services;

public record TeacherDashboardDto(
    int ExamsAwaitingReview,
    decimal ClassAverage,
    int ActiveStudents,
    int ReportsReadyForReview,
    List<StudentAtRiskDto> NeedsAttentionList,
    List<RecentSubmissionDto> RecentSubmissions
);

public record StudentAtRiskDto(Guid StudentId, string StudentName, decimal OverallAverage);
public record RecentSubmissionDto(Guid ExamAttemptId, Guid StudentId, string StudentName, string ExamTitle, DateTime SubmittedAt, decimal? Score);

public interface ITeacherDashboardService
{
    Task<TeacherDashboardDto> GetDashboardDataAsync(Guid teacherId, CancellationToken cancellationToken = default);
}

public record StudentDashboardDto(
    decimal OverallAverage,
    List<string> UrgentAlerts,
    List<DailyLessonDto> DailyLessons,
    List<UpcomingExamDto> UpcomingExams,
    DateTime? LastActivityDate,
    int CurrentStreak
);

public record DailyLessonDto(Guid MaterialId, string Title);
public record UpcomingExamDto(Guid ExamId, string Title, DateTime StartDate, DateTime? EndDate);

public interface IStudentDashboardService
{
    Task<StudentDashboardDto> GetDashboardDataAsync(Guid studentId, CancellationToken cancellationToken = default);
}
