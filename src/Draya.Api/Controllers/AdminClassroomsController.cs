using Draya.Application.Classrooms.Commands.Admin;
using Draya.Application.Classrooms.DTOs;
using Draya.Application.Classrooms.Queries.Admin;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Draya.Api.Controllers;

[ApiController]
[Route("api/v1/admin/classrooms")]
[Authorize(Roles = "SuperAdmin")]
[Produces("application/json")]
public class AdminClassroomsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminClassroomsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // Classroom Types
    
    [HttpGet("types")]
    [ProducesResponseType(typeof(List<ClassroomTypeDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ClassroomTypeDto>>> GetClassroomTypes(CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(new GetClassroomTypesQuery(), cancellationToken));
    }

    [HttpGet("types/{id:guid}")]
    [ProducesResponseType(typeof(ClassroomTypeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClassroomTypeDto>> GetClassroomTypeById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetClassroomTypeByIdQuery(id), cancellationToken);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpPost("types")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateClassroomType([FromBody] CreateClassroomTypeRequest request, CancellationToken cancellationToken)
    {
        var id = await _mediator.Send(new CreateClassroomTypeCommand(request.Name, request.Description), cancellationToken);
        return CreatedAtAction(nameof(GetClassroomTypeById), new { id }, new { id });
    }

    [HttpPut("types/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateClassroomType(Guid id, [FromBody] UpdateClassroomTypeRequest request, CancellationToken cancellationToken)
    {
        var success = await _mediator.Send(new UpdateClassroomTypeCommand(id, request.Name, request.Description, request.IsActive), cancellationToken);
        if (!success) return NotFound();
        return NoContent();
    }

    [HttpDelete("types/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivateClassroomType(Guid id, CancellationToken cancellationToken)
    {
        var success = await _mediator.Send(new DeactivateClassroomTypeCommand(id), cancellationToken);
        if (!success) return NotFound();
        return NoContent();
    }

    // Grade Levels

    [HttpGet("grade-levels")]
    [ProducesResponseType(typeof(List<GradeLevelDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<GradeLevelDto>>> GetGradeLevels(CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(new GetGradeLevelsQuery(), cancellationToken));
    }

    [HttpGet("grade-levels/{id:guid}")]
    [ProducesResponseType(typeof(GradeLevelDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GradeLevelDto>> GetGradeLevelById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetGradeLevelByIdQuery(id), cancellationToken);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpPost("grade-levels")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateGradeLevel([FromBody] CreateGradeLevelRequest request, CancellationToken cancellationToken)
    {
        var id = await _mediator.Send(new CreateGradeLevelCommand(request.Name, request.Description, request.SortOrder), cancellationToken);
        return CreatedAtAction(nameof(GetGradeLevelById), new { id }, new { id });
    }

    [HttpPut("grade-levels/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateGradeLevel(Guid id, [FromBody] UpdateGradeLevelRequest request, CancellationToken cancellationToken)
    {
        var success = await _mediator.Send(new UpdateGradeLevelCommand(id, request.Name, request.Description, request.SortOrder, request.IsActive), cancellationToken);
        if (!success) return NotFound();
        return NoContent();
    }

    [HttpDelete("grade-levels/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivateGradeLevel(Guid id, CancellationToken cancellationToken)
    {
        var success = await _mediator.Send(new DeactivateGradeLevelCommand(id), cancellationToken);
        if (!success) return NotFound();
        return NoContent();
    }
}

public record CreateClassroomTypeRequest(string Name, string Description);
public record UpdateClassroomTypeRequest(string Name, string Description, bool IsActive);
public record CreateGradeLevelRequest(string Name, string Description, int SortOrder);
public record UpdateGradeLevelRequest(string Name, string Description, int SortOrder, bool IsActive);
