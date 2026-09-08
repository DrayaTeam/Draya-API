using Draya.Application.Notifications.Commands.ClearAllNotifications;
using Draya.Application.Notifications.Commands.DeleteNotification;
using Draya.Application.Notifications.Commands.MarkAllNotificationsAsRead;
using Draya.Application.Notifications.Commands.MarkNotificationAsRead;
using Draya.Application.Notifications.Queries.GetNotifications;
using Draya.Application.Notifications.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Draya.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public NotificationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    private Guid GetCurrentUserId()
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);

        if (Guid.TryParse(userIdString, out var userId))
        {
            return userId;
        }

        throw new UnauthorizedAccessException("Invalid or missing user ID in token.");
    }

    [HttpGet]
    public async Task<ActionResult<GetNotificationsResponse>> GetNotifications(
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 20, 
        [FromQuery] bool unreadOnly = false)
    {
        var userId = GetCurrentUserId();
        var query = new GetNotificationsQuery(userId, page, pageSize, unreadOnly);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpPut("{id}/read")]
    public async Task<IActionResult> MarkAsRead(Guid id)
    {
        var userId = GetCurrentUserId();
        var command = new MarkNotificationAsReadCommand(id, userId);
        await _mediator.Send(command);
        return NoContent();
    }

    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var userId = GetCurrentUserId();
        var command = new MarkAllNotificationsAsReadCommand(userId);
        await _mediator.Send(command);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteNotification(Guid id)
    {
        var userId = GetCurrentUserId();
        var command = new DeleteNotificationCommand(id, userId);
        await _mediator.Send(command);
        return NoContent();
    }

    [HttpDelete]
    public async Task<IActionResult> ClearAllNotifications()
    {
        var userId = GetCurrentUserId();
        var command = new ClearAllNotificationsCommand(userId);
        await _mediator.Send(command);
        return NoContent();
    }
}
