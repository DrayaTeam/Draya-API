using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Draya.Domain.Classrooms;
using Draya.Domain.Exams;
using Draya.Domain.Identity;
using Draya.Domain.Reports;
using Draya.Infrastructure.Dashboards.Services;
using Draya.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Draya.Infrastructure.Tests.Dashboards;

public class TeacherDashboardServiceTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly TeacherDashboardService _service;

    public TeacherDashboardServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        _dbContext = new ApplicationDbContext(options); // ignoring dispatcher for tests
        _service = new TeacherDashboardService(_dbContext);
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }

    [Fact]
    public async Task GetDashboardDataAsync_ShouldAggregateDataCorrectly()
    {
        // Arrange
        var teacherId = Guid.NewGuid();
        var student1Id = Guid.NewGuid();
        var student2Id = Guid.NewGuid();
        var subjectId = Guid.NewGuid();
        var classroomId = Guid.NewGuid();
        
        // Setup Students
        _dbContext.Students.Add(new Student { UserId = student1Id, FullName = "Student One", ParentGuardianEmail = "stu1@test.com" });
        _dbContext.Students.Add(new Student { UserId = student2Id, FullName = "Student Two", ParentGuardianEmail = "stu2@test.com" });
        
        // Setup Classroom & Enrollments
        var subject = new Subject { Id = subjectId, Name = "Science" };
        var classroom = new Classroom { Id = classroomId, TeacherId = teacherId, SubjectId = subject.Id, Name = "Class 1", ClassroomTypeId = Guid.NewGuid(), GradeLevelId = Guid.NewGuid() };
        
        var enrollment1 = new Enrollment { StudentId = student1Id, ClassroomId = classroom.Id, Status = EnrollmentStatus.Active };
        var enrollment2 = new Enrollment { StudentId = student2Id, ClassroomId = classroom.Id, Status = EnrollmentStatus.Active };
        
        _dbContext.Subjects.Add(subject);
        _dbContext.Classrooms.Add(classroom);
        _dbContext.Enrollments.AddRange(enrollment1, enrollment2);
        
        // Setup Exam and Attempts
        var exam = new Exam(classroom.Id, Guid.NewGuid(), "Finals", "Physics", 60, DateTime.UtcNow.AddDays(-1), null, 1);
        _dbContext.Exams.Add(exam);

        // Attempt 1: Submitted, Graded (Score 40%) -> Needs Attention
        var attempt1 = new StudentExamAttempt(exam.Id, student1Id);
        attempt1.Submit();
        attempt1.UpdateFinalScore(40m, 100m, false); // Graded, no longer needs review
        
        // Attempt 2: Submitted, Awaiting Review
        var attempt2 = new StudentExamAttempt(exam.Id, student2Id);
        attempt2.Submit();
        // Not graded yet, so FinalScore is null. NeedsTeacherReview should be true.
        var prop = typeof(StudentExamAttempt).GetProperty("NeedsTeacherReview");
        if (prop != null && prop.CanWrite) prop.SetValue(attempt2, true);

        _dbContext.StudentExamAttempts.AddRange(attempt1, attempt2);

        // Setup Reports (1 ready for review)
        var report = new PerformanceReport(student1Id, attempt1.Id, teacherId, "Teacher One");
        _dbContext.PerformanceReports.Add(report);

        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _service.GetDashboardDataAsync(teacherId, CancellationToken.None);

        // Assert
        Assert.Equal(2, result.ActiveStudents);
        
        // Attempt2 is awaiting review
        Assert.Equal(1, result.ExamsAwaitingReview);
        
        // Only Attempt1 has a final score
        Assert.Equal(40m, result.ClassAverage);
        
        // Report for Attempt1 is unapproved
        Assert.Equal(1, result.ReportsReadyForReview);

        // Student 1 is at risk (< 50%)
        Assert.Single(result.NeedsAttentionList);
        Assert.Equal(student1Id, result.NeedsAttentionList[0].StudentId);
        Assert.Equal("Student One", result.NeedsAttentionList[0].StudentName);
        Assert.Equal(40m, result.NeedsAttentionList[0].OverallAverage);

        // Both attempts are recent submissions
        Assert.Equal(2, result.RecentSubmissions.Count);
    }
}
