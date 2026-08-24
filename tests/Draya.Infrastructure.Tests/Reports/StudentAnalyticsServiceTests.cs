using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Draya.Domain.Classrooms;
using Draya.Domain.Exams;
using Draya.Domain.Reports;
using Draya.Infrastructure.Persistence;
using Draya.Infrastructure.Reports.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Draya.Infrastructure.Tests.Reports;

public class StudentAnalyticsServiceTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly StudentAnalyticsService _service;

    public StudentAnalyticsServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        _dbContext = new ApplicationDbContext(options); // ignoring dispatcher for tests
        _service = new StudentAnalyticsService(_dbContext);
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }

    [Fact]
    public async Task GetAnalyticsAsync_ShouldCalculateProficiencyCorrectly()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var teacherId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();
        var classroomId = Guid.NewGuid();
        
        var subject = new Subject { Id = subjectId, Name = "Mathematics" };
        var classroom = new Classroom { Id = classroomId, TeacherId = teacherId, SubjectId = subject.Id, Name = "Class A", ClassroomTypeId = Guid.NewGuid(), GradeLevelId = Guid.NewGuid() };
        var exam = new Exam(classroom.Id, Guid.NewGuid(), "Midterm", "Algebra", 60, DateTime.UtcNow.AddDays(-1), null, 1);
        
        var question1 = new ExamQuestion(exam.Id, "1+1=?", "MultipleChoice", "Easy", "", null);
        var question2 = new ExamQuestion(exam.Id, "Solve for x", "MultipleChoice", "Hard", "", null);
        exam.AddQuestion(question1);
        exam.AddQuestion(question2);
        
        var attempt = new StudentExamAttempt(exam.Id, studentId);
        attempt.Submit();
        attempt.UpdateFinalScore(75m, 100m, false); // 75% overall on this attempt, no review needed

        // question1 got 100% (Score 1.0), question2 got 0% (Score 0.0) -> Topic average 50%
        var answer1 = new StudentAnswer(attempt.Id, question1.Id, "2");
        answer1.SetGradingResult(new Draya.Domain.Exams.AnswerGradingResult(answer1.Id, 1.0m, 1.0m, null, "Correct", false, false));
        
        var answer2 = new StudentAnswer(attempt.Id, question2.Id, "3");
        answer2.SetGradingResult(new Draya.Domain.Exams.AnswerGradingResult(answer2.Id, 0.0m, 1.0m, null, "Incorrect", false, false));

        var weaknessTopicId = Draya.Application.Utils.GuidUtility.Create(Draya.Application.Utils.GuidUtility.IsoOidNamespace, "Algebra");
        var weakness = new Draya.Domain.Reports.StudentWeakness(studentId, weaknessTopicId, "Algebra", 50m);

        _dbContext.Subjects.Add(subject);
        _dbContext.Classrooms.Add(classroom);
        _dbContext.Exams.Add(exam);
        _dbContext.ExamQuestions.AddRange(question1, question2);
        _dbContext.StudentExamAttempts.Add(attempt);
        _dbContext.StudentAnswers.AddRange(answer1, answer2);
        _dbContext.StudentWeaknesses.Add(weakness);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _service.GetAnalyticsAsync(studentId, null, CancellationToken.None);

        // Assert
        Assert.Equal(75m, result.OverallAverage);
        Assert.Equal(75m, result.HighestScore);
        Assert.Equal(1, result.CompletedExams);
        
        Assert.Single(result.SubjectProficiencies);
        Assert.Equal("Mathematics", result.SubjectProficiencies[0].SubjectName);
        Assert.Equal(50m, result.SubjectProficiencies[0].ProficiencyPercent); // (1.0 + 0.0) / 2 = 0.5 * 100 = 50%

        Assert.Single(result.WeakTopics);
        var weakTopic = result.WeakTopics.First();
        Assert.Equal("Algebra", weakTopic.TopicName);
        Assert.Equal("Mathematics", weakTopic.SubjectName);
        Assert.Equal(50m, weakTopic.ProficiencyPercent);
        // 50m is not < 50m, so it falls into "Improving" branch (< 75m)
        Assert.Equal(ProficiencyStatus.Improving, weakTopic.Status);
        
        Assert.Single(weakTopic.ExampleIncorrectAnswers);
        Assert.Contains("Solve for x", weakTopic.ExampleIncorrectAnswers[0]);
    }
}
