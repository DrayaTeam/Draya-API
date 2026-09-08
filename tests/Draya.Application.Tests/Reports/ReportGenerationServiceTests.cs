using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Draya.Application.AI;
using Draya.Application.AI.Models;
using Draya.Application.Common.Interfaces;
using Draya.Application.Reports.Events;
using Draya.Application.Reports.Services;
using Draya.Domain.Reports;
using Draya.Domain.Exams;
using Draya.Domain.Classrooms;
using Draya.Domain.Identity;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Draya.Application.Tests.Reports;

public class ReportGenerationServiceTests
{
    private readonly Mock<IStudentAnalyticsService> _analyticsServiceMock;
    private readonly Mock<ILLMService> _llmServiceMock;
    private readonly Mock<IPiiAnonymizer> _piiAnonymizerMock;
    private readonly Mock<IPerformanceReportRepository> _reportRepositoryMock;
    private readonly Mock<IStudentExamAttemptRepository> _attemptRepoMock;
    private readonly Mock<IExamRepository> _examRepoMock;
    private readonly Mock<IClassroomRepository> _classroomRepoMock;
    private readonly Mock<ITeacherRepository> _teacherRepoMock;
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Mock<ILogger<ReportGenerationService>> _loggerMock;
    private readonly ReportGenerationService _service;

    public ReportGenerationServiceTests()
    {
        _analyticsServiceMock = new Mock<IStudentAnalyticsService>();
        _llmServiceMock = new Mock<ILLMService>();
        _piiAnonymizerMock = new Mock<IPiiAnonymizer>();
        _reportRepositoryMock = new Mock<IPerformanceReportRepository>();
        _attemptRepoMock = new Mock<IStudentExamAttemptRepository>();
        _examRepoMock = new Mock<IExamRepository>();
        _classroomRepoMock = new Mock<IClassroomRepository>();
        _teacherRepoMock = new Mock<ITeacherRepository>();
        _mediatorMock = new Mock<IMediator>();
        _loggerMock = new Mock<ILogger<ReportGenerationService>>();

        _service = new ReportGenerationService(
            _analyticsServiceMock.Object,
            _llmServiceMock.Object,
            _piiAnonymizerMock.Object,
            _reportRepositoryMock.Object,
            _attemptRepoMock.Object,
            _examRepoMock.Object,
            _classroomRepoMock.Object,
            _teacherRepoMock.Object,
            _mediatorMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task GenerateReportAsync_ShouldProcessAnalyticsAndSaveReport()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var examAttemptId = Guid.NewGuid();
        var examId = Guid.NewGuid();
        var classroomId = Guid.NewGuid();
        var teacherId = Guid.NewGuid();
        
        _attemptRepoMock.Setup(x => x.GetByIdAsync(examAttemptId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StudentExamAttempt(examId, studentId));
            
        _examRepoMock.Setup(x => x.GetByIdAsync(examId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exam(classroomId, Guid.NewGuid(), "F", "F", 10, DateTime.UtcNow, null, 1));
            
        _classroomRepoMock.Setup(x => x.GetByIdAsync(classroomId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Classroom { Id = classroomId, TeacherId = teacherId });
            
        _teacherRepoMock.Setup(x => x.GetByUserIdAsync(teacherId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Teacher { UserId = teacherId, FullName = "Test Teacher" });

        var weakTopics = new List<WeakTopicResult>
        {
            new("Math Basics", "Math", 40m, ProficiencyStatus.NeedsUrgentImprovement, new List<string> { "1+1=3" }),
            new("Algebra", "Math", 60m, ProficiencyStatus.Improving, new List<string>())
        };

        var analyticsDto = new StudentAnalyticsDto(
            OverallAverage: 75.5m,
            OverallAverageMax: 100m,
            HighestScore: 90m,
            HighestScoreMax: 100m,
            CompletedExams: 5,
            SubjectProficiencies: new List<SubjectProficiencyResult> { new("Math", 70m) },
            TrendPoints: new List<TrendPointResult>(),
            WeakTopics: weakTopics,
            TotalQuestionsAsked: 10,
            TotalQuestionsReplied: 5,
            AverageExamDurationMinutes: 20m,
            CompletedLessons: 10,
            ClassroomPercentile: 80m
        );

        _analyticsServiceMock.Setup(x => x.GetAnalyticsAsync(studentId, teacherId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(analyticsDto);

        _piiAnonymizerMock.Setup(x => x.GetAnonymizedIdAsync(studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());

        _llmServiceMock.Setup(x => x.GenerateAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LlmResponse { Content = "{ \"recommendation\": \"Try harder.\" }" });

        PerformanceReport savedReport = null!;
        _reportRepositoryMock.Setup(x => x.AddAsync(It.IsAny<PerformanceReport>(), It.IsAny<CancellationToken>()))
            .Callback<PerformanceReport, CancellationToken>((r, ct) => savedReport = r)
            .Returns(Task.CompletedTask);

        // Act
        await _service.GenerateReportAsync(studentId, examAttemptId, CancellationToken.None);

        // Assert
        _reportRepositoryMock.Verify(x => x.AddAsync(It.IsAny<PerformanceReport>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.NotNull(savedReport);
        Assert.Equal(studentId, savedReport.StudentId);
        Assert.Equal(teacherId, savedReport.TeacherId);
        Assert.Equal("Test Teacher", savedReport.TeacherName);
        Assert.Equal(2, savedReport.WeakTopics.Count);
        
        var urgent = savedReport.WeakTopics.Single(x => x.TopicName == "Math Basics");
        Assert.Equal("Try harder.", urgent.Recommendation);

        var improving = savedReport.WeakTopics.Single(x => x.TopicName == "Algebra");
        Assert.Equal("Keep practicing this topic to reach full proficiency.", improving.Recommendation);

        Assert.Contains("75.5", savedReport.SummaryText);

        _mediatorMock.Verify(x => x.Publish(It.IsAny<ReportGeneratedEvent>(), It.IsAny<CancellationToken>()), Times.Once);
        _mediatorMock.Verify(x => x.Publish(It.IsAny<StudentAtRiskEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GenerateReportAsync_ShouldUseFallbackRecommendation_WhenLlmFails()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var examAttemptId = Guid.NewGuid();
        var examId = Guid.NewGuid();
        var classroomId = Guid.NewGuid();
        var teacherId = Guid.NewGuid();
        
        _attemptRepoMock.Setup(x => x.GetByIdAsync(examAttemptId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StudentExamAttempt(examId, studentId));
            
        _examRepoMock.Setup(x => x.GetByIdAsync(examId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exam(classroomId, Guid.NewGuid(), "F", "F", 10, DateTime.UtcNow, null, 1));
            
        _classroomRepoMock.Setup(x => x.GetByIdAsync(classroomId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Classroom { Id = classroomId, TeacherId = teacherId });
            
        _teacherRepoMock.Setup(x => x.GetByUserIdAsync(teacherId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Teacher { UserId = teacherId, FullName = "Test Teacher" });

        var weakTopics = new List<WeakTopicResult>
        {
            new("Math Basics", "Math", 40m, ProficiencyStatus.NeedsUrgentImprovement, new List<string>())
        };

        var analyticsDto = new StudentAnalyticsDto(
            OverallAverage: 75.5m,
            OverallAverageMax: 100m,
            HighestScore: 90m,
            HighestScoreMax: 100m,
            CompletedExams: 5,
            SubjectProficiencies: new List<SubjectProficiencyResult>(),
            TrendPoints: new List<TrendPointResult>(),
            WeakTopics: weakTopics,
            TotalQuestionsAsked: 10,
            TotalQuestionsReplied: 5,
            AverageExamDurationMinutes: 20m,
            CompletedLessons: 10,
            ClassroomPercentile: 80m
        );

        _analyticsServiceMock.Setup(x => x.GetAnalyticsAsync(studentId, teacherId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(analyticsDto);

        _llmServiceMock.Setup(x => x.GenerateAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("LLM Error"));

        PerformanceReport savedReport = null!;
        _reportRepositoryMock.Setup(x => x.AddAsync(It.IsAny<PerformanceReport>(), It.IsAny<CancellationToken>()))
            .Callback<PerformanceReport, CancellationToken>((r, ct) => savedReport = r);

        // Act
        await _service.GenerateReportAsync(studentId, examAttemptId, CancellationToken.None);

        // Assert
        Assert.NotNull(savedReport);
        var urgent = savedReport.WeakTopics.Single(x => x.TopicName == "Math Basics");
        Assert.Equal("Review the material again.", urgent.Recommendation);
    }
}
