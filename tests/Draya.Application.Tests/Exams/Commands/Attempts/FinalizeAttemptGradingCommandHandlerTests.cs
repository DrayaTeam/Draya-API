using System;
using System.Threading;
using System.Threading.Tasks;
using Draya.Application.Exams.Commands.Attempts;
using Draya.Domain.Exams;
using Moq;
using Xunit;

namespace Draya.Application.Tests.Exams.Commands.Attempts;

public class FinalizeAttemptGradingCommandHandlerTests
{
    private readonly Mock<IStudentExamAttemptRepository> _attemptRepoMock;
    private readonly FinalizeAttemptGradingCommandHandler _handler;

    public FinalizeAttemptGradingCommandHandlerTests()
    {
        _attemptRepoMock = new Mock<IStudentExamAttemptRepository>();
        _handler = new FinalizeAttemptGradingCommandHandler(_attemptRepoMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldCallRepositoryWithCorrectParameters()
    {
        // Arrange
        var attemptId = Guid.NewGuid();
        var command = new FinalizeAttemptGradingCommand(attemptId);

        _attemptRepoMock
            .Setup(repo => repo.FinalizeAttemptAndWeaknessesAsync(attemptId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result);
        _attemptRepoMock.Verify(repo => repo.FinalizeAttemptAndWeaknessesAsync(attemptId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnFalse_WhenRepositoryFails()
    {
        // Arrange
        var attemptId = Guid.NewGuid();
        var command = new FinalizeAttemptGradingCommand(attemptId);

        _attemptRepoMock
            .Setup(repo => repo.FinalizeAttemptAndWeaknessesAsync(attemptId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result);
    }
}
