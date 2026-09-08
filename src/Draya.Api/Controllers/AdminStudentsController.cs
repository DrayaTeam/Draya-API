using Draya.Application.Classrooms.DTOs;
using Draya.Application.Identity.DTOs;
using Draya.Application.Identity.Queries.GetAdminStudents;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Draya.Api.Controllers;

[ApiController]
[Route("api/v1/admin/students")]
[Authorize(Roles = "SuperAdmin,Admin")]
[Produces("application/json")]
public class AdminStudentsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminStudentsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<AdminStudentDto>>> GetStudents(
        [FromQuery] string? q,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new GetAdminStudentsQuery(q, page, pageSize);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }
}
