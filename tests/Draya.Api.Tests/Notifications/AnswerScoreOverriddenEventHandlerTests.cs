using System;
using System.Threading;
using System.Threading.Tasks;
using Draya.Api.Notifications;
using Draya.Application.Exams.Events;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Draya.Api.Tests.Notifications;

public class AnswerScoreOverriddenEventHandlerTests
{
    [Fact]
    public async Task Handle_BroadcastsToStudentGroup()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var attemptId = Guid.NewGuid();
        var answerId = Guid.NewGuid();
        var newScore = 5.0m;

        var notification = new AnswerScoreOverriddenEvent(studentId, attemptId, answerId, newScore);

        var hubContextMock = new Mock<IHubContext<NotificationHub>>();
        var clientsMock = new Mock<IHubClients>();
        var clientProxyMock = new Mock<IClientProxy>();
        var loggerMock = new Mock<ILogger<AnswerScoreOverriddenEventHandler>>();

        hubContextMock.Setup(h => h.Clients).Returns(clientsMock.Object);
        clientsMock.Setup(c => c.Group($"User_{studentId}")).Returns(clientProxyMock.Object);

        var handler = new AnswerScoreOverriddenEventHandler(hubContextMock.Object, loggerMock.Object);

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert
        clientProxyMock.Verify(c => c.SendCoreAsync(
            "AnswerScoreOverridden",
            It.Is<object[]>(args => 
                args.Length == 1 &&
                args[0].GetType().GetProperty("AttemptId") != null &&
                args[0].GetType().GetProperty("AnswerId") != null &&
                args[0].GetType().GetProperty("NewScore") != null
            ),
            It.IsAny<CancellationToken>()
        ), Times.Once);
    }
}
