using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Draya.Application.Exams.Queries.GetStudentExams;
using Draya.Domain.Classrooms;
using Draya.Domain.Exams;
using Moq;
using Xunit;

namespace Draya.Application.Tests.Exams.Queries;

public class GetStudentExamsQueryHandlerTests
{
    private readonly Mock<IExamRepository> _mockExamRepository;
    private readonly Mock<IClassroomRepository> _mockClassroomRepository;
    private readonly Mock<IStudentExamAttemptRepository> _mockAttemptRepository;
    private readonly GetStudentExamsQueryHandler _handler;

    public GetStudentExamsQueryHandlerTests()
    {
        _mockExamRepository = new Mock<IExamRepository>();
        _mockClassroomRepository = new Mock<IClassroomRepository>();
        _mockAttemptRepository = new Mock<IStudentExamAttemptRepository>();
        
        _handler = new GetStudentExamsQueryHandler(
            _mockExamRepository.Object,
            _mockClassroomRepository.Object,
            _mockAttemptRepository.Object
        );
    }

    [Fact]
    public async Task Handle_ReturnsMappedExams_WithAttempts()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var classroomId = Guid.NewGuid();
        var examId = Guid.NewGuid();
        
        _mockClassroomRepository
            .Setup(x => x.GetEnrolledClassroomIdsAsync(studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Guid> { classroomId });

        var exams = new List<Exam>
        {
            new Exam(classroomId, Guid.NewGuid(), "Test Exam", "Test Topic", 60, DateTime.UtcNow, null, 3)
        };
        // The reflection/private setter workaround since ID is private init/set
        typeof(Exam).GetProperty("Id")?.SetValue(exams[0], examId);

        _mockExamRepository
            .Setup(x => x.GetQueryable())
            .Returns(exams.AsQueryable());

        var attempt = new StudentExamAttempt(examId, studentId);
        attempt.Submit();
        attempt.UpdateFinalScore(85m, 100m, false);

        _mockAttemptRepository
            .Setup(x => x.GetAttemptsByStudentAndExamsAsync(studentId, It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StudentExamAttempt> { attempt });

        var query = new GetStudentExamsQuery(studentId, 1, 10);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
        
        var summary = result.Items.First();
        Assert.Equal(examId, summary.Id);
        Assert.Equal("Completed", summary.AttemptStatus);
        Assert.Equal(85m, summary.LatestScore);
        Assert.Equal(1, summary.UsedAttempts);
        
        Assert.NotNull(summary.Attempts);
        Assert.Single(summary.Attempts);
        
        var attemptSummary = summary.Attempts.First();
        Assert.Equal(attempt.Id, attemptSummary.Id);
        Assert.Equal(85m, attemptSummary.FinalScore);
        Assert.False(attemptSummary.NeedsTeacherReview);
        Assert.NotNull(attemptSummary.SubmittedAt);
    }
}
