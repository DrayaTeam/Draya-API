using Draya.Application.Identity.Commands.UpdateStudentProfile;
using Draya.Application.Identity.Commands.UploadProfilePicture;
using Draya.Api.Controllers.Identity.Requests;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Draya.Api.Controllers;
using Draya.Application.Exams.DTOs;
using Draya.Application.Exams.Queries.GetStudentExamById;


[ApiController]
[Route("api/v1/students")]
[Authorize(Roles = "Student")]
[Produces("application/json")]
public class StudentsController : ControllerBase
{
    private readonly IMediator _mediator;

    public StudentsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPut("profile")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateProfile(
        [FromBody] UpdateStudentProfileRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();

        var command = new UpdateStudentProfileCommand(
            userId, 
            request.FullName, 
            request.ParentGuardianEmail, 
            request.ParentGuardianName,
            request.ParentGuardianPhone,
            request.DateOfBirth);
            
        await _mediator.Send(command, cancellationToken);
        
        return NoContent();
    }

    [HttpPost("profile/picture")]
    [Consumes("multipart/form-data")]
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
        var command = new UploadStudentProfilePictureCommand(
            userId,
            file.OpenReadStream(),
            file.FileName,
            file.ContentType);

        var url = await _mediator.Send(command, cancellationToken);
        return Ok(new { profilePictureUrl = url });
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);

        if (!Guid.TryParse(userIdClaim, out var userId))
            throw new UnauthorizedAccessException();

        return userId;
    }

    [HttpGet("exams/{id:guid}")]
    [ProducesResponseType(typeof(StudentExamDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetExamById(Guid id)
    {
        var exam = await _mediator.Send(new GetStudentExamByIdQuery(id));
        
        if (exam == null)
            return NotFound(new { message = $"Exam with ID {id} not found." });
            
        return Ok(exam);
    }

    [HttpGet("exams")]
    [ProducesResponseType(typeof(Draya.Application.Classrooms.DTOs.PagedResult<StudentExamSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStudentExams(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();
        var query = new Draya.Application.Exams.Queries.GetStudentExams.GetStudentExamsQuery(userId, page, pageSize);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }
}


