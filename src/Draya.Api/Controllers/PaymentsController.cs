using Draya.Application.Payments.Commands.ProcessPaymobWebhook;
using Draya.Application.Payments.Commands.RefundPayment;
using Draya.Application.Payments.Services;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Draya.Api.Controllers;

[ApiController]
[Route("api/v1/payments")]
[Produces("application/json")]
public class PaymentsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IPaymobService _paymobService;

    public PaymentsController(IMediator mediator, IPaymobService paymobService)
    {
        _mediator = mediator;
        _paymobService = paymobService;
    }

    private Guid GetAdminId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (!Guid.TryParse(value, out var id)) throw new UnauthorizedAccessException();
        return id;
    }

    [HttpPost("webhook")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ProcessWebhook(
        [FromBody] PaymobWebhookRequest request,
        [FromQuery] string? hmac,
        CancellationToken cancellationToken)
    {
        // Verify HMAC if present or in request payload
        if (!string.IsNullOrWhiteSpace(hmac) || !string.IsNullOrWhiteSpace(request.Hmac))
        {
            var receivedHmac = hmac ?? request.Hmac!;
            var queryDict = Request.Query.ToDictionary(q => q.Key, q => q.Value.ToString());
            
            if (!_paymobService.VerifyHmac(queryDict, receivedHmac))
            {
                return Unauthorized(new { error = "Invalid Paymob HMAC signature." });
            }
        }

        var command = new ProcessPaymobWebhookCommand(
            request.PaymentTransactionId,
            request.IsSuccess,
            request.RawPayload ?? string.Empty
        );

        await _mediator.Send(command, cancellationToken);
        return Ok(new { status = "received" });
    }

    [HttpPost("{id:guid}/refund")]
    [Authorize(Roles = "SuperAdmin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RefundPayment(Guid id, CancellationToken cancellationToken)
    {
        var adminId = GetAdminId();
        var command = new RefundPaymentCommand(id, adminId);
        await _mediator.Send(command, cancellationToken);
        return Ok(new { message = "Payment transaction refunded successfully." });
    }
}

public record PaymobWebhookRequest(
    Guid PaymentTransactionId,
    bool IsSuccess,
    string? Hmac,
    string? RawPayload
);
