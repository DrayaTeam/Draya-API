using Draya.Application.Identity.Commands.UpdateStudentProfile;
using Draya.Application.Identity.Commands.UploadProfilePicture;
using Draya.Api.Controllers.Identity.Requests;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Draya.Api.Controllers;

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
}

