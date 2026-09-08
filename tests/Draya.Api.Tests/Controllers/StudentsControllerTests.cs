using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Draya.Api.Controllers;
using Draya.Application.Classrooms.DTOs;
using Draya.Application.Exams.DTOs;
using Draya.Application.Exams.Queries.GetStudentExams;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Draya.Api.Tests.Controllers;

public class StudentsControllerTests
{
    private readonly Mock<IMediator> _mockMediator;
    private readonly StudentsController _controller;

    public StudentsControllerTests()
    {
        _mockMediator = new Mock<IMediator>();
        _controller = new StudentsController(_mockMediator.Object);
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
    public async Task GetStudentExams_ReturnsOk_WithMappedExamsAndAttempts()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        SetupUser(studentId);

        var attemptSummary = new StudentExamAttemptSummaryDto(
            Guid.NewGuid(), 
            90m, 
            100m,
            false, 
            DateTime.UtcNow, 
            DateTime.UtcNow.AddMinutes(-30)
        );

        var examSummary = new StudentExamSummaryDto(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Algebra Quiz",
            "Algebra",
            60,
            DateTime.UtcNow.AddDays(-1),
            null,
            2,
            DateTime.UtcNow.AddDays(-5),
            true,
            "Completed",
            90m,
            100m,
            1,
            new List<StudentExamAttemptSummaryDto> { attemptSummary }
        );

        var pagedResult = new PagedResult<StudentExamSummaryDto>(
            new List<StudentExamSummaryDto> { examSummary },
            1,
            20,
            1,
            1
        );

        _mockMediator
            .Setup(m => m.Send(It.Is<GetStudentExamsQuery>(q => q.StudentId == studentId && q.Page == 1 && q.PageSize == 20), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _controller.GetStudentExams(1, 20, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var value = Assert.IsType<PagedResult<StudentExamSummaryDto>>(okResult.Value);
        
        Assert.Single(value.Items);
        var item = value.Items[0];
        Assert.Equal("Algebra Quiz", item.Title);
        Assert.NotNull(item.Attempts);
        Assert.Single(item.Attempts);
        Assert.Equal(90m, item.Attempts[0].FinalScore);
    }
}
