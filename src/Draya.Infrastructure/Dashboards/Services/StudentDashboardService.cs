using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Draya.Application.Dashboards.Services;
using Draya.Application.Reports.Services;
using Draya.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Draya.Infrastructure.Dashboards.Services;

public class StudentDashboardService : IStudentDashboardService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IStudentAnalyticsService _analyticsService;

    public StudentDashboardService(ApplicationDbContext dbContext, IStudentAnalyticsService analyticsService)
    {
        _dbContext = dbContext;
        _analyticsService = analyticsService;
    }

    public async Task<StudentDashboardDto> GetDashboardDataAsync(Guid studentId, CancellationToken cancellationToken = default)
    {
        // 1. Overall Average & Urgent Alerts from Analytics Service
        var analytics = await _analyticsService.GetAnalyticsAsync(studentId, cancellationToken);
        var overallAverage = analytics.OverallAverage;
        
        var urgentAlerts = analytics.WeakTopics
            .Where(w => w.Status == "Needs urgent improvement")
            .Select(w => $"Urgent review needed for {w.TopicName} ({w.SubjectName}).")
            .ToList();

        // 2. Upcoming Exams
        var enrolledClassrooms = _dbContext.Enrollments
            .Where(e => e.StudentId == studentId && e.Status == Draya.Domain.Classrooms.EnrollmentStatus.Active)
            .Select(e => e.ClassroomId);

        var now = DateTime.UtcNow;

        var upcomingExams = await _dbContext.Exams
            .Where(e => enrolledClassrooms.Contains(e.ClassroomId) && e.StartDate > now)
            .OrderBy(e => e.StartDate)
            .Take(5)
            .Select(e => new UpcomingExamDto(e.Id, e.Title, e.StartDate, e.EndDate))
            .ToListAsync(cancellationToken);

        // 3. Daily Lessons (Assume any material added in the last 7 days is a "daily lesson")
        var sevenDaysAgo = now.AddDays(-7);
        var dailyLessons = await _dbContext.LearningMaterials
            .Where(m => enrolledClassrooms.Contains(m.ClassroomId) && m.CreatedAt >= sevenDaysAgo)
            .OrderByDescending(m => m.CreatedAt)
            .Take(5)
            .Select(m => new DailyLessonDto(m.Id, m.Title))
            .ToListAsync(cancellationToken);

        // 4. Streak Tracking (Added Activity tracking in Student entity)
        var student = await _dbContext.Students.FirstOrDefaultAsync(s => s.UserId == studentId, cancellationToken);
        var lastActivity = student?.LastActivityDate;
        var streak = student?.CurrentStreak ?? 0;

        return new StudentDashboardDto(
            overallAverage,
            urgentAlerts,
            dailyLessons,
            upcomingExams,
            lastActivity,
            streak
        );
    }
}
