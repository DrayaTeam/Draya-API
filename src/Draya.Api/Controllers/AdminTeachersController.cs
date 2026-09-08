using Draya.Application.Identity.DTOs;
using Draya.Application.Identity.Queries.SearchTeachersForAdmin;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Draya.Api.Controllers;

[ApiController]
[Route("api/v1/admin/teachers")]
[Authorize(Roles = "SuperAdmin,Admin")]
[Produces("application/json")]
public class AdminTeachersController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminTeachersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("search")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<TeacherSearchDto>>> SearchTeachers(
        [FromQuery] string? q,
        CancellationToken cancellationToken)
    {
        var query = new SearchTeachersForAdminQuery(q);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }
}
