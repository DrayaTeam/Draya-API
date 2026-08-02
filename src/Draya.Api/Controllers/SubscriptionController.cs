using Draya.Application.Subscriptions.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
namespace Draya.Api.Controllers;
[ApiController]
[Route("api/v1/subscription")]
[Authorize(Roles = "Teacher")]
public class SubscriptionController(IMediator mediator) : ControllerBase
{
    [HttpGet("current")] public async Task<IActionResult> GetCurrent(CancellationToken ct) => Ok(await mediator.Send(new GetCurrentSubscriptionQuery(GetTeacherId()), ct));
    [HttpGet("usage")] public async Task<IActionResult> GetUsage(CancellationToken ct) => Ok(await mediator.Send(new GetSubscriptionUsageQuery(GetTeacherId()), ct));
    private Guid GetTeacherId() { var value=User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub); if (!Guid.TryParse(value,out var id)) throw new UnauthorizedAccessException(); return id; }
}
