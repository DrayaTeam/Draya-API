using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Draya.Application.Dashboards.Services;
using Draya.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Draya.Infrastructure.Dashboards.Services;

public class TeacherDashboardService : ITeacherDashboardService
{
    private readonly ApplicationDbContext _dbContext;

    public TeacherDashboardService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<TeacherDashboardDto> GetDashboardDataAsync(Guid teacherId, CancellationToken cancellationToken = default)
    {
        var teacherClassrooms = _dbContext.Classrooms.Where(c => c.TeacherId == teacherId).Select(c => c.Id);

        // Exams awaiting review
        var awaitingReviewQuery = _dbContext.StudentExamAttempts
            .Where(a => a.IsSubmitted && a.NeedsTeacherReview)
            .Join(_dbContext.Exams, a => a.ExamId, e => e.Id, (a, e) => new { Attempt = a, Exam = e })
            .Where(x => teacherClassrooms.Contains(x.Exam.ClassroomId));
            
        int examsAwaitingReview = await awaitingReviewQuery.CountAsync(cancellationToken);

        // Class average
        var allAttemptsQuery = _dbContext.StudentExamAttempts
            .Where(a => a.IsSubmitted && a.FinalScore != null)
            .Join(_dbContext.Exams, a => a.ExamId, e => e.Id, (a, e) => new { Attempt = a, Exam = e })
            .Where(x => teacherClassrooms.Contains(x.Exam.ClassroomId));

        decimal classAverage = 0;
        int totalExams = await allAttemptsQuery.CountAsync(cancellationToken);
        if (totalExams > 0)
        {
            classAverage = await allAttemptsQuery.AverageAsync(x => x.Attempt.FinalScore!.Value, cancellationToken);
        }

        // Active students (enrolled in teacher's classrooms)
        int activeStudents = await _dbContext.Enrollments
            .Where(e => teacherClassrooms.Contains(e.ClassroomId) && e.Status == Draya.Domain.Classrooms.EnrollmentStatus.Active)
            .Select(e => e.StudentId)
            .Distinct()
            .CountAsync(cancellationToken);

        // AI reports ready for review count
        var reportsReady = await _dbContext.PerformanceReports
            .Where(r => !r.IsApproved)
            .Join(_dbContext.StudentExamAttempts, r => r.ExamAttemptId, a => a.Id, (r, a) => new { Report = r, Attempt = a })
            .Join(_dbContext.Exams, x => x.Attempt.ExamId, e => e.Id, (x, e) => new { x.Report, x.Attempt, Exam = e })
            .Where(x => teacherClassrooms.Contains(x.Exam.ClassroomId))
            .CountAsync(cancellationToken);

        // Needs Attention List (Average score < 50%)
        // Get all students enrolled
        var enrolledStudents = await _dbContext.Enrollments
            .Where(e => teacherClassrooms.Contains(e.ClassroomId) && e.Status == Draya.Domain.Classrooms.EnrollmentStatus.Active)
            .Select(e => e.StudentId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var needsAttention = new System.Collections.Generic.List<StudentAtRiskDto>();
        if (enrolledStudents.Any())
        {
            var studentAverages = await _dbContext.StudentExamAttempts
                .Where(a => a.IsSubmitted && a.FinalScore != null && enrolledStudents.Contains(a.StudentId))
                .GroupBy(a => a.StudentId)
                .Select(g => new { StudentId = g.Key, AvgScore = g.Average(a => a.FinalScore!.Value) })
                .Where(s => s.AvgScore < 50m)
                .Take(5)
                .ToListAsync(cancellationToken);

            var studentIds = studentAverages.Select(s => s.StudentId).ToList();
            var studentsData = await _dbContext.Students.Where(s => studentIds.Contains(s.UserId)).ToDictionaryAsync(s => s.UserId, s => s.FullName, cancellationToken);

            foreach(var s in studentAverages)
            {
                if (studentsData.TryGetValue(s.StudentId, out var name))
                {
                    needsAttention.Add(new StudentAtRiskDto(s.StudentId, name, s.AvgScore));
                }
            }
        }

        // Recent submissions
        var recentSubmissionsData = await _dbContext.StudentExamAttempts
            .Where(a => a.IsSubmitted)
            .Join(_dbContext.Exams, a => a.ExamId, e => e.Id, (a, e) => new { Attempt = a, Exam = e })
            .Where(x => teacherClassrooms.Contains(x.Exam.ClassroomId))
            .OrderByDescending(x => x.Attempt.SubmittedAt)
            .Take(5)
            .ToListAsync(cancellationToken);

        var rsStudentIds = recentSubmissionsData.Select(r => r.Attempt.StudentId).Distinct().ToList();
        var rsStudentsData = await _dbContext.Students.Where(s => rsStudentIds.Contains(s.UserId)).ToDictionaryAsync(s => s.UserId, s => s.FullName, cancellationToken);

        var recentSubmissions = recentSubmissionsData.Select(x => new RecentSubmissionDto(
            x.Attempt.Id,
            x.Attempt.StudentId,
            rsStudentsData.TryGetValue(x.Attempt.StudentId, out var n) ? n : "Unknown",
            x.Exam.Title,
            x.Attempt.SubmittedAt!.Value,
            x.Attempt.FinalScore
        )).ToList();

        // New Messages Count (Placeholder for now since we don't have a messages domain)
        int newMessagesCount = 0;

        // Weekly Submissions Activity (Last 7 days)
        var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);
        var weeklyDataRaw = await _dbContext.StudentExamAttempts
            .Where(a => a.IsSubmitted && a.SubmittedAt >= sevenDaysAgo)
            .Join(_dbContext.Exams, a => a.ExamId, e => e.Id, (a, e) => new { Attempt = a, Exam = e })
            .Where(x => teacherClassrooms.Contains(x.Exam.ClassroomId))
            .ToListAsync(cancellationToken);

        var weeklySubmissionsActivity = weeklyDataRaw
            .GroupBy(x => x.Attempt.SubmittedAt!.Value.DayOfWeek)
            .Select(g => new DailySubmissionActivityDto(
                GetArabicDayName(g.Key),
                g.Count(),
                g.Any(x => x.Attempt.FinalScore.HasValue) ? g.Where(x => x.Attempt.FinalScore.HasValue).Average(x => x.Attempt.FinalScore!.Value) : 0
            ))
            .ToList();

        return new TeacherDashboardDto(
            examsAwaitingReview,
            classAverage,
            activeStudents,
            reportsReady,
            newMessagesCount,
            weeklySubmissionsActivity,
            needsAttention,
            recentSubmissions
        );
    }

    private string GetArabicDayName(DayOfWeek day)
    {
        return day switch
        {
            DayOfWeek.Sunday => "أحد",
            DayOfWeek.Monday => "إثنين",
            DayOfWeek.Tuesday => "ثلاثاء",
            DayOfWeek.Wednesday => "أربعاء",
            DayOfWeek.Thursday => "خميس",
            DayOfWeek.Friday => "جمعة",
            DayOfWeek.Saturday => "سبت",
            _ => day.ToString()
        };
    }
}
