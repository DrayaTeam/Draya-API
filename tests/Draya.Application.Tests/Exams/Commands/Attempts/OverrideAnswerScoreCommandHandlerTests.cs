using System;
using System.Threading;
using System.Threading.Tasks;
using Draya.Application.Exams.Commands.Attempts;
using Draya.Domain.Exams;
using Moq;
using Xunit;

namespace Draya.Application.Tests.Exams.Commands.Attempts;

public class OverrideAnswerScoreCommandHandlerTests
{
    private readonly Mock<IStudentExamAttemptRepository> _attemptRepoMock;
    private readonly OverrideAnswerScoreCommandHandler _handler;

    public OverrideAnswerScoreCommandHandlerTests()
    {
        _attemptRepoMock = new Mock<IStudentExamAttemptRepository>();
        _handler = new OverrideAnswerScoreCommandHandler(_attemptRepoMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldCallRepositoryWithCorrectParameters()
    {
        // Arrange
        var attemptId = Guid.NewGuid();
        var answerId = Guid.NewGuid();
        var teacherId = Guid.NewGuid();
        var newScore = 5.0m;
        
        var command = new OverrideAnswerScoreCommand(attemptId, answerId, teacherId, newScore);

        _attemptRepoMock
            .Setup(repo => repo.OverrideAnswerScoreAsync(attemptId, answerId, newScore, teacherId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result);
        _attemptRepoMock.Verify(repo => repo.OverrideAnswerScoreAsync(attemptId, answerId, newScore, teacherId, It.IsAny<CancellationToken>()), Times.Once);
    }
    
    [Fact]
    public async Task Handle_ShouldReturnFalse_WhenRepositoryFails()
    {
        // Arrange
        var command = new OverrideAnswerScoreCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 10m);

        _attemptRepoMock
            .Setup(repo => repo.OverrideAnswerScoreAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result);
    }
}
