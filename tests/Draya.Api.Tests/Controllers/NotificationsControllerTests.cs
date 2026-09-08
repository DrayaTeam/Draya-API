using System;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Draya.Api.Tests.Infrastructure;
using Draya.Domain.Notifications;
using Draya.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Draya.Api.Tests.Controllers;

public class NotificationsControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory<Program> _factory;

    public NotificationsControllerTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<(Guid userId, Guid notificationId)> SetupTestDataAsync()
    {
        var userId = Guid.NewGuid();
        var notificationId = Guid.NewGuid();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var notification = new Notification
        {
            Id = notificationId,
            UserId = userId,
            Title = "Test API Notification",
            Message = "This is a test notification for integration tests.",
            Type = NotificationType.Info,
            Read = false
        };

        db.Notifications.Add(notification);
        await db.SaveChangesAsync();

        return (userId, notificationId);
    }

    [Fact]
    public async Task GetNotifications_ReturnsOk_WithData()
    {
        // Arrange
        var (userId, _) = await SetupTestDataAsync();
        
        // Mock authentication
        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("X-Test-UserId", userId.ToString());
        _client.DefaultRequestHeaders.Add("X-Test-Role", "Student");

        // Act
        var response = await _client.GetAsync("/api/v1/notifications");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        
        Assert.True(result.TryGetProperty("items", out var items));
        Assert.Equal(1, items.GetArrayLength());
        
        Assert.True(result.TryGetProperty("totalCount", out var totalCount));
        Assert.Equal(1, totalCount.GetInt32());
    }

    [Fact]
    public async Task MarkAsRead_UpdatesNotificationToRead()
    {
        // Arrange
        var (userId, notificationId) = await SetupTestDataAsync();
        
        // Mock authentication
        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("X-Test-UserId", userId.ToString());
        _client.DefaultRequestHeaders.Add("X-Test-Role", "Student");

        // Act
        var response = await _client.PutAsync($"/api/v1/notifications/{notificationId}/read", null);

        // Assert
        response.EnsureSuccessStatusCode();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var updatedNotification = await db.Notifications.FindAsync(notificationId);
        
        Assert.NotNull(updatedNotification);
        Assert.True(updatedNotification.Read);
    }
}
