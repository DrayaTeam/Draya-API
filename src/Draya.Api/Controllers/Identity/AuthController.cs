using Draya.Api.Controllers.Identity.Requests;
using Draya.Application.Identity.Commands.ChangePassword;
using Draya.Application.Identity.Commands.Login;
using Draya.Application.Identity.Commands.Logout;
using Draya.Application.Identity.Commands.RequestPasswordReset;
using Draya.Application.Identity.Commands.ConfirmPasswordReset;
using Draya.Application.Identity.Commands.RefreshToken;
using Draya.Application.Identity.Commands.RegisterStudent;
using Draya.Application.Identity.Commands.RegisterTeacher;
using Draya.Application.Identity.Queries.GetMyProfile;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Draya.Api.Controllers.Identity;

/// <summary>
/// Authentication endpoints — matches API_CONTRACT.md §2 exactly.
/// </summary>
[ApiController]
[Route("api/v1/auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>POST /api/v1/auth/register/teacher — Teacher self-registration.</summary>
    [HttpPost("register/teacher")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RegisterTeacher([FromBody] RegisterTeacherRequest request, CancellationToken cancellationToken)
    {
        var command = new RegisterTeacherCommand(request.Email, request.Password, request.ConfirmPassword, request.FullName, request.Phone, request.Specialization, request.Description);
        var result = await _mediator.Send(command, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>POST /api/v1/auth/register/student — Student self-registration.</summary>
    [HttpPost("register/student")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RegisterStudent([FromBody] RegisterStudentRequest request, CancellationToken cancellationToken)
    {
        var command = new RegisterStudentCommand(
            request.Email, request.Password, request.ConfirmPassword, request.FullName,
            request.ParentGuardianEmail, request.DateOfBirth);
        var result = await _mediator.Send(command, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>POST /api/v1/auth/login — Returns access + refresh tokens.</summary>
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var command = new LoginCommand(request.Email, request.Password);
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>POST /api/v1/auth/refresh-token — Rotates refresh token and issues new pair.</summary>
    [HttpPost("refresh-token")]
    [HttpPost("/api/auth/refresh")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var command = new RefreshTokenCommand(request.RefreshToken);
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>POST /api/v1/auth/logout — Revokes the current refresh token.</summary>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);

        if (!Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var command = new LogoutCommand(userId, string.Empty);
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpPost("password-reset/request")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> RequestPasswordReset([FromBody] PasswordResetRequest request, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new RequestPasswordResetCommand(request.Email), cancellationToken));

    [HttpPost("password-reset/confirm")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ConfirmPasswordReset([FromBody] PasswordResetConfirmationRequest request, CancellationToken cancellationToken)
    {
        await _mediator.Send(new ConfirmPasswordResetCommand(request.Token, request.NewPassword), cancellationToken);
        return NoContent();
    }

    /// <summary>POST /api/v1/auth/change-password — Changes password for the authenticated user.</summary>
    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);

        if (!Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var command = new ChangePasswordCommand(userId, request.CurrentPassword, request.NewPassword, request.ConfirmPassword);
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>GET /api/v1/auth/me — Returns the caller's own profile (role-specific shape).</summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyProfile(CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);

        if (!Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var query = new GetMyProfileQuery(userId);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }
}
