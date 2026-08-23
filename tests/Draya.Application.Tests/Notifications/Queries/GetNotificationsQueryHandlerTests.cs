using Draya.Application.Notifications.Queries.GetNotifications;
using Draya.Domain.Notifications;
using Moq;
using Xunit;

namespace Draya.Application.Tests.Notifications.Queries;

public class GetNotificationsQueryHandlerTests
{
    private readonly Mock<INotificationRepository> _repositoryMock;
    private readonly GetNotificationsQueryHandler _handler;

    public GetNotificationsQueryHandlerTests()
    {
        _repositoryMock = new Mock<INotificationRepository>();
        _handler = new GetNotificationsQueryHandler(_repositoryMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnPaginatedNotifications()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var query = new GetNotificationsQuery(userId, 1, 10, false);

        var mockNotifications = new List<Notification>
        {
            new Notification { Id = Guid.NewGuid(), Title = "N1", UserId = userId, Type = NotificationType.Info },
            new Notification { Id = Guid.NewGuid(), Title = "N2", UserId = userId, Type = NotificationType.Success }
        };

        _repositoryMock
            .Setup(repo => repo.GetByUserIdAsync(userId, 1, 10, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockNotifications);

        _repositoryMock
            .Setup(repo => repo.GetTotalCountAsync(userId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        _repositoryMock
            .Setup(repo => repo.GetUnreadCountAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Items.Count());
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.UnreadCount);
        
        var firstItem = result.Items.First();
        Assert.Equal("N1", firstItem.Title);
        Assert.Equal("info", firstItem.Type);
    }
}
