using Draya.Application.Classrooms.Commands.CreateSection;
using Draya.Application.Classrooms.Commands.DeleteSection;
using Draya.Application.Classrooms.Commands.UpdateSection;
using Draya.Application.Classrooms.Queries.GetClassroomSections;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Draya.Api.Controllers;

[ApiController]
[Route("api/classrooms")]
public class ClassroomSectionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ClassroomSectionsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("{classroomId:guid}/sections")]
    [Authorize(Roles = "Teacher,Student")]
    public async Task<IActionResult> GetSections(Guid classroomId)
    {
        var result = await _mediator.Send(new GetClassroomSectionsQuery(classroomId));
        return Ok(result);
    }

    [HttpPost("{classroomId:guid}/sections")]
    [Authorize(Roles = "Teacher")]
    public async Task<IActionResult> CreateSection(Guid classroomId, [FromBody] CreateSectionRequest request)
    {
        var teacherId = GetUserId();
        var command = new CreateSectionCommand(classroomId, teacherId, request.Title, request.Description, request.Order);
        var result = await _mediator.Send(command);
        return Ok(new { SectionId = result });
    }

    [HttpPut("sections/{sectionId:guid}")]
    [Authorize(Roles = "Teacher")]
    public async Task<IActionResult> UpdateSection(Guid sectionId, [FromBody] UpdateSectionRequest request)
    {
        var teacherId = GetUserId();
        var command = new UpdateSectionCommand(sectionId, teacherId, request.Title, request.Description, request.Order);
        await _mediator.Send(command);
        return NoContent();
    }

    [HttpDelete("sections/{sectionId:guid}")]
    [Authorize(Roles = "Teacher")]
    public async Task<IActionResult> DeleteSection(Guid sectionId)
    {
        var teacherId = GetUserId();
        await _mediator.Send(new DeleteSectionCommand(sectionId, teacherId));
        return NoContent();
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);

        if (!Guid.TryParse(userIdClaim, out var userId))
            throw new UnauthorizedAccessException();

        return userId;
    }
}

public record CreateSectionRequest(string Title, string Description, int Order);
public record UpdateSectionRequest(string Title, string Description, int Order);
