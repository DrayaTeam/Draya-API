using System;
using System.Threading.Tasks;
using Draya.Application.Reports.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Draya.Api.Controllers.v1;

[ApiController]
[Route("api/v1/students")]
public class StudentAnalyticsController : ControllerBase
{
    private readonly IStudentAnalyticsService _analyticsService;

    public StudentAnalyticsController(IStudentAnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;
    }

    /// <summary>
    /// Gets the full analytics profile for a student.
    /// </summary>
    [HttpGet("{studentId}/analytics")]
    [Authorize(Roles = "Student,Teacher")]
    [ProducesResponseType(typeof(StudentAnalyticsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStudentAnalytics(Guid studentId)
    {
        // Simple auth check to ensure a student can only view their own analytics
        if (User.IsInRole("Student"))
        {
            var loggedInId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (loggedInId != studentId.ToString())
                return Forbid();
        }

        var data = await _analyticsService.GetAnalyticsAsync(studentId);
        return Ok(data);
    }
}
