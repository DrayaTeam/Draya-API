using Draya.Api.Controllers.Admin.Requests;
using Draya.Application.Identity.Commands.UpdateAdminProfile;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Draya.Api.Controllers;

[ApiController]
[Route("api/v1/admin/profile")]
[Authorize(Roles = "Admin,SuperAdmin")]
[Produces("application/json")]
public class AdminProfileController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminProfileController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>PUT /api/v1/admin/profile — Updates admin profile information.</summary>
    [HttpPut]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateProfile(
        [FromBody] UpdateAdminProfileRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var command = new UpdateAdminProfileCommand(userId, request.FullName, request.Email, request.PhoneNumber);
        await _mediator.Send(command, cancellationToken);
        
        // Note: Returning Ok() with empty payload or updated object. Frontend expects 200 OK.
        return Ok();
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
