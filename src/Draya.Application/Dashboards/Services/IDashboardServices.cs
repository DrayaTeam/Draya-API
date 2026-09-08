using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Draya.Application.Dashboards.Services;

public record TeacherDashboardDto(
    int ExamsAwaitingReview,
    decimal ClassAverage,
    decimal ClassAverageMax,
    int ActiveStudents,
    int ReportsReadyForReview,
    int NewMessagesCount,
    List<DailySubmissionActivityDto> WeeklySubmissionsActivity,
    List<StudentAtRiskDto> NeedsAttentionList,
    List<RecentSubmissionDto> RecentSubmissions
);

public record DailySubmissionActivityDto(string DayOfWeek, int SubmissionsCount, decimal AverageScore, decimal AverageMaxScore);
public record StudentAtRiskDto(Guid StudentId, string StudentName, decimal OverallAverage, decimal OverallAverageMax);
public record RecentSubmissionDto(Guid ExamAttemptId, Guid StudentId, string StudentName, string ExamTitle, DateTime SubmittedAt, decimal? Score, decimal? MaxScore);

public interface ITeacherDashboardService
{
    Task<TeacherDashboardDto> GetDashboardDataAsync(Guid teacherId, CancellationToken cancellationToken = default);
}

public record StudentDashboardDto(
    decimal OverallAverage,
    decimal OverallAverageMax,
    int CompletedLessonsCount,
    int SubscribedPackagesCount,
    List<string> UrgentAlerts,
    List<DailyLessonDto> DailyLessons,
    List<UpcomingExamDto> UpcomingExams,
    List<PointOfFocusDto> PointsNeedingFocus,
    DateTime? LastActivityDate,
    int CurrentStreak
);

public record DailyLessonDto(Guid MaterialId, string Title, int CompletedLectures, int TotalLectures);
public record PointOfFocusDto(string TopicName, decimal ProficiencyPercent);
public record UpcomingExamDto(Guid ExamId, string Title, DateTime StartDate, DateTime? EndDate);

public interface IStudentDashboardService
{
    Task<StudentDashboardDto> GetDashboardDataAsync(Guid studentId, CancellationToken cancellationToken = default);
}
