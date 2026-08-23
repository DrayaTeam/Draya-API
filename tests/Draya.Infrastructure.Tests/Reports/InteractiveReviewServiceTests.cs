using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Draya.Application.AI;
using Draya.Application.AI.Models;
using Draya.Application.Exams.Services;
using Draya.Application.Materials.RAG;
using Draya.Domain.Classrooms;
using Draya.Domain.Reports;
using Draya.Infrastructure.Persistence;
using Draya.Infrastructure.Reports.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Draya.Infrastructure.Tests.Reports;

public class InteractiveReviewServiceTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly Mock<IRetrievalService> _mockRetrievalService;
    private readonly Mock<IExamGenerationService> _mockExamGenService;
    private readonly Mock<ILLMService> _mockLlmService;
    private readonly InteractiveReviewService _service;

    public InteractiveReviewServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        _dbContext = new ApplicationDbContext(options);
        
        _mockRetrievalService = new Mock<IRetrievalService>();
        _mockExamGenService = new Mock<IExamGenerationService>();
        _mockLlmService = new Mock<ILLMService>();
        
        _service = new InteractiveReviewService(
            _dbContext, 
            _mockRetrievalService.Object, 
            _mockExamGenService.Object,
            _mockLlmService.Object);
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }

    [Fact]
    public async Task GetRevisionAsync_ShouldReturnRecommendationAndMaterials()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var topicName = "Algebra";
        
        var topicId = Draya.Application.Utils.GuidUtility.Create(Draya.Application.Utils.GuidUtility.IsoOidNamespace, topicName);
        var weakness = new StudentWeakness(studentId, topicId, topicName, 45m);
        _dbContext.StudentWeaknesses.Add(weakness);

        // 2. Setup Classroom & Enrollment
        var classroom = new Classroom { Id = Guid.NewGuid(), TeacherId = Guid.NewGuid(), SubjectId = Guid.NewGuid(), Name = "Math Class", ClassroomTypeId = Guid.NewGuid(), GradeLevelId = Guid.NewGuid() };
        var enrollment = new Enrollment { StudentId = studentId, ClassroomId = classroom.Id, Status = EnrollmentStatus.Active };
        _dbContext.Classrooms.Add(classroom);
        _dbContext.Enrollments.Add(enrollment);

        // 3. Setup Learning Material
        var material = new Draya.Domain.Materials.LearningMaterial { Id = Guid.NewGuid(), ClassroomId = classroom.Id, Title = "Math Book" };
        var version = new Draya.Domain.Materials.MaterialVersion { Id = Guid.NewGuid(), MaterialId = material.Id, VersionNumber = 1 };
        material.Versions.Add(version);
        _dbContext.LearningMaterials.Add(material);
        
        await _dbContext.SaveChangesAsync();

        _mockRetrievalService
            .Setup(x => x.SearchAsync(It.IsAny<RetrievalQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RetrievedChunk>
            {
                new RetrievedChunk { ChunkId = Guid.NewGuid().ToString(), Text = "Quadratic formula is -b +- sqrt(b^2 - 4ac)...", Score = 0.95f }
            });
            
        _mockLlmService
            .Setup(x => x.GenerateAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LlmResponse { Content = "AI Explanation" });

        // Act
        var result = await _service.GetRevisionAsync(studentId, topicName, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Review the material related to this topic.", result.Recommendation);
        Assert.NotNull(result.AiExplanation);
        
        // Verify RAG was called with correct material version ID
        _mockRetrievalService.Verify(x => x.SearchAsync(
            It.Is<RetrievalQuery>(q => q.QueryText == "Algebra" && q.MaterialVersionIds.Contains(version.Id)), 
            It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    [Fact]
    public async Task GetRevisionAsync_ShouldReturnFallback_WhenNoReportOrTopicFound()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var topicName = "Unknown Topic";
        
        // Act
        var result = await _service.GetRevisionAsync(studentId, topicName, CancellationToken.None);

        // Assert
        Assert.Equal("No weakness found for this topic.", result.Recommendation);
        Assert.Equal("You are currently not marked as weak in this topic.", result.AiExplanation);
    }

    [Fact]
    public async Task GeneratePracticeExamAsync_ShouldGenerateExamAndReturnJobId()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var topicName = "Algebra";
        var expectedJobId = Guid.NewGuid();
        
        var classroom = new Classroom { Id = Guid.NewGuid(), TeacherId = Guid.NewGuid(), SubjectId = Guid.NewGuid(), Name = "Math Class", ClassroomTypeId = Guid.NewGuid(), GradeLevelId = Guid.NewGuid() };
        var enrollment = new Enrollment { StudentId = studentId, ClassroomId = classroom.Id, Status = EnrollmentStatus.Active };
        _dbContext.Classrooms.Add(classroom);
        _dbContext.Enrollments.Add(enrollment);
        await _dbContext.SaveChangesAsync();

        var request = new Draya.Application.Reports.Services.PracticeExamRequest();

        _mockExamGenService
            .Setup(x => x.StartGenerationAsync(It.IsAny<GenerateExamRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedJobId);

        // Act
        var jobId = await _service.GeneratePracticeExamAsync(studentId, topicName, request, CancellationToken.None);

        // Assert
        Assert.Equal(expectedJobId, jobId);
        
        _mockExamGenService.Verify(x => x.StartGenerationAsync(
            It.Is<GenerateExamRequest>(r => 
                r.ClassroomId == classroom.Id &&
                r.Topic == "Algebra" &&
                r.IsPracticeReview == true &&
                r.QuestionRequirements.Count == 2 &&
                r.QuestionRequirements.Any(req => req.Type == "MultipleChoice" && req.Count == 3) &&
                r.QuestionRequirements.Any(req => req.Type == "TrueFalse" && req.Count == 2)),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GeneratePracticeExamAsync_ShouldThrowException_WhenStudentNotEnrolled()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var request = new Draya.Application.Reports.Services.PracticeExamRequest();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<Exception>(() => 
            _service.GeneratePracticeExamAsync(studentId, "Algebra", request, CancellationToken.None));
        
        Assert.Equal("Student is not enrolled in any classrooms.", ex.Message);
    }
}
