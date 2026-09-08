using System;
using System.Threading;
using System.Threading.Tasks;
using Draya.Application.Exams.Commands.Attempts;
using Draya.Application.Exams.Events;
using Draya.Domain.Exams;
using MediatR;
using Moq;
using Xunit;

namespace Draya.Application.Tests.Exams.Commands.Attempts;

public class OverrideAnswerScoreCommandHandlerTests
{
    private readonly Mock<IStudentExamAttemptRepository> _attemptRepoMock;
    private readonly Mock<IMediator> _mediatorMock;
    private readonly OverrideAnswerScoreCommandHandler _handler;

    public OverrideAnswerScoreCommandHandlerTests()
    {
        _attemptRepoMock = new Mock<IStudentExamAttemptRepository>();
        _mediatorMock = new Mock<IMediator>();
        _handler = new OverrideAnswerScoreCommandHandler(_attemptRepoMock.Object, _mediatorMock.Object);
    }

    [Fact]
    public async Task Handle_AttemptNotFound_ReturnsFalse()
    {
        // Arrange
        var command = new OverrideAnswerScoreCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 10m);
        _attemptRepoMock.Setup(repo => repo.GetByIdAsync(command.AttemptId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((StudentExamAttempt?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result);
        _mediatorMock.Verify(m => m.Publish(It.IsAny<AnswerScoreOverriddenEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_OverrideFails_ReturnsFalse()
    {
        // Arrange
        var command = new OverrideAnswerScoreCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 10m);
        var attempt = new StudentExamAttempt(Guid.NewGuid(), Guid.NewGuid());
        _attemptRepoMock.Setup(repo => repo.GetByIdAsync(command.AttemptId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(attempt);
        _attemptRepoMock.Setup(repo => repo.OverrideAnswerScoreAsync(command.AttemptId, command.AnswerId, command.NewScore, command.TeacherId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result);
        _mediatorMock.Verify(m => m.Publish(It.IsAny<AnswerScoreOverriddenEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_OverrideSucceeds_PublishesEventAndReturnsTrue()
    {
        // Arrange
        var command = new OverrideAnswerScoreCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 10m);
        var attempt = new StudentExamAttempt(Guid.NewGuid(), Guid.NewGuid());
        _attemptRepoMock.Setup(repo => repo.GetByIdAsync(command.AttemptId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(attempt);
        _attemptRepoMock.Setup(repo => repo.OverrideAnswerScoreAsync(command.AttemptId, command.AnswerId, command.NewScore, command.TeacherId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result);
        _mediatorMock.Verify(m => m.Publish(It.Is<AnswerScoreOverriddenEvent>(e =>
            e.StudentId == attempt.StudentId &&
            e.AttemptId == command.AttemptId &&
            e.AnswerId == command.AnswerId &&
            e.NewScore == command.NewScore
        ), It.IsAny<CancellationToken>()), Times.Once);
    }
}
