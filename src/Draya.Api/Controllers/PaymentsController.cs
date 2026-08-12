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
    [HttpGet("webhook")]
    [HttpPost("callback")]
    [HttpGet("callback")]
    [AllowAnonymous]
    public async Task<IActionResult> ProcessWebhook(CancellationToken cancellationToken)
    {
        string rawBody = string.Empty;
        if (HttpMethods.IsPost(Request.Method))
        {
            using var reader = new StreamReader(Request.Body);
            rawBody = await reader.ReadToEndAsync(cancellationToken);
        }

        Guid transactionId = Guid.Empty;
        bool isSuccess = false;
        bool foundStatus = false;

        // 1. Try parsing JSON body
        if (!string.IsNullOrWhiteSpace(rawBody))
        {
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(rawBody);
                var root = doc.RootElement;
                var target = root.TryGetProperty("obj", out var objElem) ? objElem : root;

                string? refStr = null;
                if (target.TryGetProperty("special_reference", out var specRef)) refStr = specRef.GetString();
                else if (target.TryGetProperty("merchant_order_id", out var merchRef)) refStr = merchRef.GetString();
                else if (target.TryGetProperty("paymentTransactionId", out var payTxId)) refStr = payTxId.GetString();
                else if (target.TryGetProperty("order", out var orderElem))
                {
                    if (orderElem.TryGetProperty("merchant_order_id", out var orderMerchRef)) refStr = orderMerchRef.GetString();
                    else if (orderElem.TryGetProperty("special_reference", out var orderSpecRef)) refStr = orderSpecRef.GetString();
                }

                if (Guid.TryParse(refStr, out var parsedGuid))
                {
                    transactionId = parsedGuid;
                }

                if (target.TryGetProperty("success", out var succElem))
                {
                    if (succElem.ValueKind == System.Text.Json.JsonValueKind.True || succElem.ValueKind == System.Text.Json.JsonValueKind.False)
                    {
                        isSuccess = succElem.GetBoolean();
                        foundStatus = true;
                    }
                    else if (succElem.ValueKind == System.Text.Json.JsonValueKind.String && bool.TryParse(succElem.GetString(), out var succBool))
                    {
                        isSuccess = succBool;
                        foundStatus = true;
                    }
                }
                else if (target.TryGetProperty("isSuccess", out var isSuccElem))
                {
                    isSuccess = isSuccElem.GetBoolean();
                    foundStatus = true;
                }
            }
            catch
            {
                // Fallback to query params
            }
        }

        // 2. Query Parameters fallback
        if (transactionId == Guid.Empty)
        {
            var qRef = Request.Query["special_reference"].ToString();
            if (string.IsNullOrEmpty(qRef)) qRef = Request.Query["merchant_order_id"].ToString();
            if (string.IsNullOrEmpty(qRef)) qRef = Request.Query["paymentTransactionId"].ToString();
            if (string.IsNullOrEmpty(qRef)) qRef = Request.Query["id"].ToString();

            if (Guid.TryParse(qRef, out var parsedQGuid))
            {
                transactionId = parsedQGuid;
            }
        }

        if (!foundStatus)
        {
            var qSuccess = Request.Query["success"].ToString();
            if (string.IsNullOrEmpty(qSuccess)) qSuccess = Request.Query["is_success"].ToString();
            if (bool.TryParse(qSuccess, out var parsedSuccess))
            {
                isSuccess = parsedSuccess;
            }
            else
            {
                isSuccess = true;
            }
        }

        if (transactionId == Guid.Empty)
        {
            return BadRequest(new { error = "Unable to determine paymentTransactionId / special_reference from request." });
        }

        var command = new ProcessPaymobWebhookCommand(
            transactionId,
            isSuccess,
            rawBody
        );

        var result = await _mediator.Send(command, cancellationToken);
        return Ok(new { status = "success", paymentTransactionId = transactionId, processed = result });
    }

    [HttpPost("confirm/{id:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ConfirmPayment(Guid id, [FromQuery] bool isSuccess = true, CancellationToken cancellationToken = default)
    {
        var command = new ProcessPaymobWebhookCommand(id, isSuccess, "Manual confirmation");
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(new { status = "confirmed", paymentTransactionId = id, processed = result });
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
