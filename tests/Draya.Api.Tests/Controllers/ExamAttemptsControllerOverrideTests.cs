using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Draya.Api.Controllers;
using Draya.Application.Exams.Commands.Attempts;
using Draya.Application.Exams.Services;
using Draya.Domain.Exams;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Draya.Api.Tests.Controllers;

public class ExamAttemptsControllerOverrideTests
{
    private readonly Mock<IExamGradingService> _examGradingServiceMock;
    private readonly Mock<IStudentExamAttemptRepository> _attemptRepoMock;
    private readonly Mock<IExamGradingJobRepository> _jobRepoMock;
    private readonly Mock<IMediator> _mediatorMock;
    private readonly ExamAttemptsController _controller;

    public ExamAttemptsControllerOverrideTests()
    {
        _examGradingServiceMock = new Mock<IExamGradingService>();
        _attemptRepoMock = new Mock<IStudentExamAttemptRepository>();
        _jobRepoMock = new Mock<IExamGradingJobRepository>();
        _mediatorMock = new Mock<IMediator>();

        _controller = new ExamAttemptsController(
            _examGradingServiceMock.Object,
            _attemptRepoMock.Object,
            _jobRepoMock.Object,
            _mediatorMock.Object);

        // Setup User context
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };
    }

    [Fact]
    public async Task OverrideAnswerScore_ReturnsNoContent_WhenMediatRReturnsTrue()
    {
        // Arrange
        var attemptId = Guid.NewGuid();
        var answerId = Guid.NewGuid();
        var request = new OverrideScoreRequestDto { NewScore = 8.5m };

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<OverrideAnswerScoreCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.OverrideAnswerScore(attemptId, answerId, request, CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task OverrideAnswerScore_ReturnsBadRequest_WhenMediatRReturnsFalse()
    {
        // Arrange
        var attemptId = Guid.NewGuid();
        var answerId = Guid.NewGuid();
        var request = new OverrideScoreRequestDto { NewScore = 8.5m };

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<OverrideAnswerScoreCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.OverrideAnswerScore(attemptId, answerId, request, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Could not override score. Check if attempt/answer exists.", badRequestResult.Value);
    }
}
