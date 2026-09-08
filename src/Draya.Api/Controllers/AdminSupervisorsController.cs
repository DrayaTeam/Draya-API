using Draya.Api.Controllers.Admin.Requests;
using Draya.Application.Identity.Commands.InviteSupervisor;
using Draya.Application.Identity.Commands.ResendSupervisorInvite;
using Draya.Application.Identity.Commands.ToggleSupervisorStatus;
using Draya.Application.Identity.Queries.GetSupervisors;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Draya.Api.Controllers;

[ApiController]
[Route("api/v1/admin/supervisors")]
[Authorize(Roles = "SuperAdmin,Admin")]
[Produces("application/json")]
public class AdminSupervisorsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminSupervisorsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("invite")]
    [Authorize(Roles = "SuperAdmin")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> InviteSupervisor(
        [FromBody] InviteSupervisorRequest request,
        CancellationToken cancellationToken)
    {
        var command = new InviteSupervisorCommand(request.Name, request.Email, request.Role);
        var result = await _mediator.Send(command, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPost("{id:guid}/resend-invite")]
    [Authorize(Roles = "SuperAdmin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ResendInvite(
        Guid id,
        CancellationToken cancellationToken)
    {
        var command = new ResendSupervisorInviteCommand(id);
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSupervisors(CancellationToken cancellationToken)
    {
        var query = new GetSupervisorsQuery();
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id:guid}/status")]
    [Authorize(Roles = "SuperAdmin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ToggleStatus(
        Guid id,
        [FromBody] ToggleSupervisorStatusRequest request,
        CancellationToken cancellationToken)
    {
        var command = new ToggleSupervisorStatusCommand(id, request.IsActive);
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }
}
