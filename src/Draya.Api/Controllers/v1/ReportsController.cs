using System;
using System.Threading.Tasks;
using Draya.Application.Reports.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Draya.Api.Controllers.v1;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Roles = "Teacher")]
public class ReportsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ReportsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Approves a performance report and sends it to the parent.
    /// </summary>
    [HttpPost("{reportId}/approve")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ApproveReport(Guid reportId)
    {
        var result = await _mediator.Send(new ApproveReportCommand(reportId));
        
        if (!result)
            return NotFound(new { message = "Report not found." });

        return Ok(new { message = "Report approved and email sent to parent." });
    }
}
