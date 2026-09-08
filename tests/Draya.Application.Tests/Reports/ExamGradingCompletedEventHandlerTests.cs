using System;
using System.Threading;
using System.Threading.Tasks;
using Draya.Application.Exams.Events;
using Draya.Application.Reports.EventHandlers;
using Draya.Application.Reports.Services;
using Draya.Domain.Exams;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Draya.Application.Tests.Reports;

public class ExamGradingCompletedEventHandlerTests
{
    private readonly Mock<IReportGenerationService> _reportGenerationServiceMock;
    private readonly Mock<ILogger<ExamGradingCompletedEventHandler>> _loggerMock;
    private readonly ExamGradingCompletedEventHandler _handler;

    public ExamGradingCompletedEventHandlerTests()
    {
        _reportGenerationServiceMock = new Mock<IReportGenerationService>();
        _loggerMock = new Mock<ILogger<ExamGradingCompletedEventHandler>>();
        _handler = new ExamGradingCompletedEventHandler(_reportGenerationServiceMock.Object, _loggerMock.Object);
    }

    [Theory]
    [InlineData(GradingStatus.Completed)]
    [InlineData(GradingStatus.CompletedWithWarning)]
    public async Task Handle_ShouldGenerateReport_WhenGradingIsCompletedOrWarning(GradingStatus status)
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var attemptId = Guid.NewGuid();
        var notification = new ExamGradingProgressEvent(Guid.NewGuid(), studentId, attemptId, status, null, 100m, false);

        // Act
        await _handler.Handle(notification, CancellationToken.None);

        // Assert
        _reportGenerationServiceMock.Verify(
            x => x.GenerateReportAsync(studentId, attemptId, It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    [Theory]
    [InlineData(GradingStatus.Pending)]
    [InlineData(GradingStatus.Grading)]
    [InlineData(GradingStatus.Failed)]
    public async Task Handle_ShouldNotGenerateReport_WhenGradingIsNotCompleted(GradingStatus status)
    {
        // Arrange
        var notification = new ExamGradingProgressEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), status, null, null, false);

        // Act
        await _handler.Handle(notification, CancellationToken.None);

        // Assert
        _reportGenerationServiceMock.Verify(
            x => x.GenerateReportAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), 
            Times.Never);
    }
}
