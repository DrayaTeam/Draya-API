using System;
using System.Threading.Tasks;
using Draya.Application.Dashboards.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Draya.Api.Controllers.v1;

[ApiController]
[Route("api/v1/dashboard/teacher")]
[Authorize(Roles = "Teacher")]
public class TeacherDashboardController : ControllerBase
{
    private readonly ITeacherDashboardService _dashboardService;

    public TeacherDashboardController(ITeacherDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    /// <summary>
    /// Gets the teacher dashboard data.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(TeacherDashboardDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDashboardData()
    {
        var teacherIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(teacherIdStr) || !Guid.TryParse(teacherIdStr, out var teacherId))
            return Unauthorized();

        var data = await _dashboardService.GetDashboardDataAsync(teacherId);
        return Ok(data);
    }
}
