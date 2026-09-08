using Draya.Application.Notifications.Commands.CreateNotification;
using Draya.Domain.Notifications;
using Draya.Application.Notifications.Events;
using MediatR;
using Moq;
using Xunit;

namespace Draya.Application.Tests.Notifications.Commands;

public class CreateNotificationCommandHandlerTests
{
    private readonly Mock<INotificationRepository> _repositoryMock;
    private readonly Mock<IMediator> _mediatorMock;
    private readonly CreateNotificationCommandHandler _handler;

    public CreateNotificationCommandHandlerTests()
    {
        _repositoryMock = new Mock<INotificationRepository>();
        _mediatorMock = new Mock<IMediator>();
        _handler = new CreateNotificationCommandHandler(_repositoryMock.Object, _mediatorMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldCreateNotificationAndPublishEvent()
    {
        // Arrange
        var command = new CreateNotificationCommand(
            Guid.NewGuid(), 
            "Test Title", 
            "Test Message", 
            NotificationType.Info, 
            "/test/link");

        _repositoryMock
            .Setup(repo => repo.AddAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test Title", result.Title);
        Assert.Equal("Test Message", result.Message);
        Assert.Equal("info", result.Type);
        Assert.Equal("/test/link", result.Link);
        Assert.False(result.Read);

        _repositoryMock.Verify(repo => repo.AddAsync(It.Is<Notification>(n => 
            n.Title == "Test Title" && n.UserId == command.UserId), It.IsAny<CancellationToken>()), Times.Once);

        _mediatorMock.Verify(m => m.Publish(It.Is<NotificationCreatedEvent>(e => 
            e.UserId == command.UserId && e.Notification.Id == result.Id), It.IsAny<CancellationToken>()), Times.Once);
    }
}
