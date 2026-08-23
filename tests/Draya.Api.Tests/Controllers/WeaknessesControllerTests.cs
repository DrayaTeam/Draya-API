using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Draya.Api.Controllers;
using Draya.Domain.Reports;
using Draya.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Draya.Api.Tests.Controllers;

public class WeaknessesControllerTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly WeaknessesController _controller;

    public WeaknessesControllerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
            
        _dbContext = new ApplicationDbContext(options);
        _controller = new WeaknessesController(_dbContext);
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }

    private void SetupUser(Guid studentId)
    {
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, studentId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };
    }

    [Fact]
    public async Task GetActiveWeaknesses_ReturnsOk_WithActiveWeaknesses()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        SetupUser(studentId);
        
        var topicId = Guid.NewGuid();
        var weakness = new StudentWeakness(studentId, topicId, "Math Algebra", 50m);
        _dbContext.StudentWeaknesses.Add(weakness);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _controller.GetActiveWeaknesses(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var value = okResult.Value as System.Collections.Generic.IEnumerable<dynamic>;
        Assert.NotNull(value);
        Assert.Single(value);
    }

    [Fact]
    public async Task GetResolvedWeaknesses_ReturnsOk_WithResolvedWeaknesses()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        SetupUser(studentId);
        
        var topicId = Guid.NewGuid();
        var weakness = new StudentWeakness(studentId, topicId, "Math Algebra", 50m);
        weakness.UpdatePerformance(100m, 80m, false);
        _dbContext.StudentWeaknesses.Add(weakness);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _controller.GetResolvedWeaknesses(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var value = okResult.Value as System.Collections.Generic.IEnumerable<dynamic>;
        Assert.NotNull(value);
        Assert.Single(value);
    }
}
