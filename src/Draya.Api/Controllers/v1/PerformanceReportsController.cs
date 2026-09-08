using System;
using System.Threading;
using System.Threading.Tasks;
using Draya.Application.Reports.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Draya.Api.Controllers.v1;

[ApiController]
[Route("api/v1/students/{studentId:guid}/performance-reports")]
[Authorize]
[Produces("application/json")]
public class PerformanceReportsController : ControllerBase
{
    private readonly IMediator _mediator;

    public PerformanceReportsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("latest")]
    [ProducesResponseType(typeof(PerformanceReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLatestPerformanceReport(
        [FromRoute] Guid studentId,
        CancellationToken cancellationToken)
    {
        var currentUserIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) 
                               ?? User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);

        if (!Guid.TryParse(currentUserIdStr, out var currentUserId))
        {
            return Unauthorized();
        }

        // Optional: Ensure the caller is either the student themselves or a teacher/admin.
        // For now, we'll let [Authorize] handle the base authentication, but we could add:
        // if (currentUserId != studentId && !User.IsInRole("Teacher") && !User.IsInRole("Admin")) return Forbid();

        var query = new GetLatestPerformanceReportQuery(studentId);
        var report = await _mediator.Send(query, cancellationToken);

        if (report == null)
        {
            return NotFound(new { message = "No performance report found for this student." });
        }

        return Ok(report);
    }
}
