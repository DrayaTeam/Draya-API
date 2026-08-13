
using Draya.Application.Identity.DTOs;
using Draya.Application.Identity.Queries.GetTeacherById;
using Draya.Application.Identity.Queries.GetTeachers;
using Draya.Application.Identity.Commands.UpdateTeacherProfile;
using Draya.Api.Controllers.Identity.Requests;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Draya.Api.Controllers;

[ApiController]
[Route("api/v1/teachers")]
[Authorize]
[Produces("application/json")]
public class TeachersController : ControllerBase
{
    private readonly IMediator _mediator;

    public TeachersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<TeacherProfileDto>>> GetTeachers(
        [FromQuery] string? specialization,
        CancellationToken cancellationToken)
    {
        var query = new GetTeachersQuery(specialization);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    [HttpPut("profile")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [Authorize(Roles = "Teacher")]
    public async Task<IActionResult> UpdateProfile(
        [FromBody] UpdateTeacherProfileRequest request,
        CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);

        if (!Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var command = new UpdateTeacherProfileCommand(userId, request.FullName, request.Phone, request.Specialization, request.Description);
        await _mediator.Send(command, cancellationToken);
        
        return NoContent();
    }

    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TeacherProfileDto>> GetTeacherById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var query = new GetTeacherByIdQuery(id);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

}
