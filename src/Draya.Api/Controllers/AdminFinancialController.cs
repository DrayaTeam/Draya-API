using Draya.Application.Admin.Commands.AdjustTeacherBalance;
using Draya.Application.Admin.Commands.ApproveWithdrawalRequest;
using Draya.Application.Admin.Commands.MarkWithdrawalAsPaid;
using Draya.Application.Admin.Commands.RejectWithdrawalRequest;
using Draya.Application.Admin.Commands.UpdatePlatformSettings;
using Draya.Application.Admin.DTOs;
using Draya.Application.Admin.Queries.GetAdminWithdrawalRequests;
using Draya.Application.Admin.Queries.GetFinancialOverview;
using Draya.Application.Admin.Queries.GetPlatformSettings;
using Draya.Application.Common.Models;
using Draya.Domain.Wallets;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Draya.Api.Controllers;

[ApiController]
[Route("api/v1/admin/financial")]
[Authorize(Roles = "SuperAdmin")]
[Produces("application/json")]
public class AdminFinancialController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminFinancialController(IMediator mediator)
    {
        _mediator = mediator;
    }

    private Guid GetAdminId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (!Guid.TryParse(value, out var id)) throw new UnauthorizedAccessException();
        return id;
    }

    [HttpGet("settings")]
    [ProducesResponseType(typeof(PlatformSettingDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PlatformSettingDto>> GetSettings(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetPlatformSettingsQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpPut("settings")]
    [ProducesResponseType(typeof(PlatformSettingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PlatformSettingDto>> UpdateSettings(
        [FromBody] UpdatePlatformSettingsRequest request,
        CancellationToken cancellationToken)
    {
        var adminId = GetAdminId();
        var command = new UpdatePlatformSettingsCommand(request.AIExamPrice, request.FreeMonthlyAIExamQuota, request.PlatformCommissionPercent, adminId);
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpGet("overview")]
    [ProducesResponseType(typeof(FinancialOverviewDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<FinancialOverviewDto>> GetFinancialOverview(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetFinancialOverviewQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("withdrawals")]
    [ProducesResponseType(typeof(PaginatedResult<AdminWithdrawalRequestDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResult<AdminWithdrawalRequestDto>>> GetWithdrawalRequests(
        [FromQuery] WithdrawalStatus? statusFilter,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetAdminWithdrawalRequestsQuery(statusFilter, pageNumber, pageSize), cancellationToken);
        return Ok(result);
    }

    [HttpPost("withdrawals/{id:guid}/approve")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ApproveWithdrawal(Guid id, CancellationToken cancellationToken)
    {
        var adminId = GetAdminId();
        await _mediator.Send(new ApproveWithdrawalRequestCommand(id, adminId), cancellationToken);
        return Ok(new { message = "Withdrawal request approved." });
    }

    [HttpPost("withdrawals/{id:guid}/reject")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RejectWithdrawal(
        Guid id,
        [FromBody] RejectWithdrawalRequest request,
        CancellationToken cancellationToken)
    {
        var adminId = GetAdminId();
        await _mediator.Send(new RejectWithdrawalRequestCommand(id, request.RejectionReason, adminId), cancellationToken);
        return Ok(new { message = "Withdrawal request rejected." });
    }

    [HttpPost("withdrawals/{id:guid}/mark-paid")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkWithdrawalPaid(
        Guid id,
        [FromBody] MarkWithdrawalPaidRequest request,
        CancellationToken cancellationToken)
    {
        var adminId = GetAdminId();
        await _mediator.Send(new MarkWithdrawalAsPaidCommand(id, request.AdminNote, adminId), cancellationToken);
        return Ok(new { message = "Withdrawal request marked as paid." });
    }

    [HttpPost("adjustments")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> MakeManualAdjustment(
        [FromBody] ManualAdjustmentRequest request,
        CancellationToken cancellationToken)
    {
        var adminId = GetAdminId();
        var command = new AdjustTeacherBalanceCommand(request.TeacherId, request.Amount, request.BalanceType, request.Reason, adminId);
        await _mediator.Send(command, cancellationToken);
        return Ok(new { message = "Manual wallet adjustment completed." });
    }

    [HttpGet("adjustments")]
    [ProducesResponseType(typeof(PaginatedResult<Draya.Application.Admin.DTOs.ManualAdjustmentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResult<Draya.Application.Admin.DTOs.ManualAdjustmentDto>>> GetManualAdjustments(
        [FromQuery] Guid? teacherId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new Draya.Application.Admin.Queries.GetManualAdjustments.GetManualAdjustmentsQuery(teacherId, pageNumber, pageSize);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }
}

public record UpdatePlatformSettingsRequest(decimal AIExamPrice, int FreeMonthlyAIExamQuota, decimal PlatformCommissionPercent);
public record RejectWithdrawalRequest(string RejectionReason);
public record MarkWithdrawalPaidRequest(string? AdminNote);
public record ManualAdjustmentRequest(Guid TeacherId, decimal Amount, WalletBalanceType BalanceType, string Reason);
