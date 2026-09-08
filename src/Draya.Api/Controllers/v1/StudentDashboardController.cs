using System;
using System.Threading.Tasks;
using Draya.Application.Dashboards.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Draya.Api.Controllers.v1;

[ApiController]
[Route("api/v1/dashboard/student")]
[Authorize(Roles = "Student")]
public class StudentDashboardController : ControllerBase
{
    private readonly IStudentDashboardService _dashboardService;

    public StudentDashboardController(IStudentDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    /// <summary>
    /// Gets the student dashboard data.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(StudentDashboardDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDashboardData()
    {
        var studentIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(studentIdStr) || !Guid.TryParse(studentIdStr, out var studentId))
            return Unauthorized();

        var data = await _dashboardService.GetDashboardDataAsync(studentId);
        return Ok(data);
    }
}
