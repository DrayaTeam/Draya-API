using System;
using System.Threading;
using System.Threading.Tasks;
using Draya.Infrastructure.AI;
using Draya.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Draya.Infrastructure.Tests.AI;

public class PiiAnonymizerTests
{
    private ApplicationDbContext GetDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new ApplicationDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    [Fact]
    public async Task GetAnonymizedIdAsync_CreatesNewMapping_WhenNoneExists()
    {
        // Arrange
        using var dbContext = GetDbContext();
        var anonymizer = new PiiAnonymizer(dbContext);
        var studentId = Guid.NewGuid();

        // Act
        var anonymizedId = await anonymizer.GetAnonymizedIdAsync(studentId, CancellationToken.None);

        // Assert
        Assert.NotEqual(Guid.Empty, anonymizedId);
        Assert.NotEqual(studentId, anonymizedId);
        
        var mapping = await dbContext.PiiMappings.FirstOrDefaultAsync(m => m.StudentId == studentId);
        Assert.NotNull(mapping);
        Assert.Equal(anonymizedId, mapping.AnonymizedId);
    }

    [Fact]
    public async Task GetAnonymizedIdAsync_ReturnsExistingMapping_WhenCalledTwice()
    {
        // Arrange
        using var dbContext = GetDbContext();
        var anonymizer = new PiiAnonymizer(dbContext);
        var studentId = Guid.NewGuid();

        // Act
        var anonymizedId1 = await anonymizer.GetAnonymizedIdAsync(studentId, CancellationToken.None);
        var anonymizedId2 = await anonymizer.GetAnonymizedIdAsync(studentId, CancellationToken.None);

        // Assert
        Assert.Equal(anonymizedId1, anonymizedId2);
        
        var mappingsCount = await dbContext.PiiMappings.CountAsync();
        Assert.Equal(1, mappingsCount);
    }

    [Fact]
    public async Task GetOriginalStudentIdAsync_ReturnsStudentId_WhenMappingExists()
    {
        // Arrange
        using var dbContext = GetDbContext();
        var anonymizer = new PiiAnonymizer(dbContext);
        var studentId = Guid.NewGuid();
        var anonymizedId = await anonymizer.GetAnonymizedIdAsync(studentId, CancellationToken.None);

        // Act
        var originalStudentId = await anonymizer.GetOriginalStudentIdAsync(anonymizedId, CancellationToken.None);

        // Assert
        Assert.True(originalStudentId.HasValue);
        Assert.Equal(studentId, originalStudentId.Value);
    }
}
