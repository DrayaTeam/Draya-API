using Draya.Application.Classrooms.DTOs;
using Draya.Application.Classrooms.Queries.GetClassroomsByTeacher;
using Draya.Application.Exams.DTOs;
using Draya.Application.Exams.Queries.GetPendingReviews;
using Draya.Application.Identity.DTOs;
using Draya.Application.Identity.Queries.GetTeacherById;
using Draya.Application.Identity.Queries.GetTeachers;
using Draya.Application.Identity.Commands.UpdateTeacherProfile;
using Draya.Application.Identity.Commands.UploadProfilePicture;
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
        var userId = GetUserId();

        var command = new UpdateTeacherProfileCommand(userId, request.FullName, request.Phone, request.Specialization, request.Description);
        await _mediator.Send(command, cancellationToken);
        
        return NoContent();
    }

    [HttpPost("profile/picture")]
    [Consumes("multipart/form-data")]
    [Authorize(Roles = "Teacher")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UploadProfilePicture(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = new { message = "File is required." } });

        var userId = GetUserId();
        var command = new UploadTeacherProfilePictureCommand(
            userId,
            file.OpenReadStream(),
            file.FileName,
            file.ContentType);

        var url = await _mediator.Send(command, cancellationToken);
        return Ok(new { profilePictureUrl = url });
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

    [HttpGet("{id}/classrooms")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ClassroomDto>>> GetClassroomsByTeacherId(
        Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new GetClassroomsByTeacherQuery(id, page, pageSize);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Returns pending teacher review attempts, grouped by classroom and paginated.
    /// </summary>
    /// <remarks>
    /// The response is a <see cref="PagedResult{T}"/> wrapper — NOT a bare array.
    /// The list of classrooms is available at <c>.items</c> on the response object.
    ///
    /// Example response shape:
    /// <code>
    /// {
    ///   "items": [ { "classroomId": "...", "classroomName": "...", "exams": [...] } ],
    ///   "page": 1,
    ///   "pageSize": 10,
    ///   "totalCount": 3,
    ///   "totalPages": 1
    /// }
    /// </code>
    /// Only classrooms that have at least one attempt with <c>NeedsTeacherReview = true</c> are returned.
    /// </remarks>
    [HttpGet("pending-reviews")]
    [Authorize(Roles = "Teacher")]
    [ProducesResponseType(typeof(PagedResult<PendingReviewClassroomDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPendingReviews(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();
        var query = new GetPendingReviewsQuery(userId, page, pageSize);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);

        if (!Guid.TryParse(userIdClaim, out var userId))
            throw new UnauthorizedAccessException();

        return userId;
    }
}

