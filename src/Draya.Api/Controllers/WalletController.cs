using Draya.Application.Common.Models;
using Draya.Application.Wallets.Commands.CreatePayoutAccount;
using Draya.Application.Wallets.Commands.DeletePayoutAccount;
using Draya.Application.Wallets.Commands.InitiateTopUp;
using Draya.Application.Wallets.Commands.RequestWithdrawal;
using Draya.Application.Wallets.Commands.UpdatePayoutAccount;
using Draya.Application.Wallets.DTOs;
using Draya.Application.Wallets.Queries.GetTeacherPayoutAccounts;
using Draya.Application.Wallets.Queries.GetTeacherWithdrawals;
using Draya.Application.Wallets.Queries.GetWalletBalance;
using Draya.Application.Wallets.Queries.GetWalletTransactions;
using Draya.Domain.Wallets;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Draya.Api.Controllers;

[ApiController]
[Route("api/v1/wallet")]
[Authorize(Roles = "Teacher")]
[Produces("application/json")]
public class WalletController : ControllerBase
{
    private readonly IMediator _mediator;

    public WalletController(IMediator mediator)
    {
        _mediator = mediator;
    }

    private Guid GetTeacherId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (!Guid.TryParse(value, out var id)) throw new UnauthorizedAccessException();
        return id;
    }

    [HttpGet("balance")]
    [ProducesResponseType(typeof(WalletBalanceDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<WalletBalanceDto>> GetBalance(CancellationToken cancellationToken)
    {
        var teacherId = GetTeacherId();
        var result = await _mediator.Send(new GetWalletBalanceQuery(teacherId), cancellationToken);
        return Ok(result);
    }

    [HttpGet("transactions")]
    [ProducesResponseType(typeof(PaginatedResult<WalletTransactionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResult<WalletTransactionDto>>> GetTransactions(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var teacherId = GetTeacherId();
        var result = await _mediator.Send(new GetWalletTransactionsQuery(teacherId, pageNumber, pageSize), cancellationToken);
        return Ok(result);
    }

    [HttpPost("topup")]
    [ProducesResponseType(typeof(TopUpCheckoutDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TopUpCheckoutDto>> InitiateTopUp(
        [FromBody] InitiateTopUpRequest request,
        CancellationToken cancellationToken)
    {
        var teacherId = GetTeacherId();
        var result = await _mediator.Send(new InitiateTopUpCommand(teacherId, request.Amount), cancellationToken);
        return Ok(result);
    }

    [HttpPost("withdrawals")]
    [ProducesResponseType(typeof(WithdrawalRequestDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<WithdrawalRequestDto>> RequestWithdrawal(
        [FromBody] RequestWithdrawalRequest request,
        CancellationToken cancellationToken)
    {
        var teacherId = GetTeacherId();
        var result = await _mediator.Send(new RequestWithdrawalCommand(teacherId, request.Amount), cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpGet("withdrawals")]
    [ProducesResponseType(typeof(PaginatedResult<WithdrawalRequestDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResult<WithdrawalRequestDto>>> GetWithdrawals(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var teacherId = GetTeacherId();
        var result = await _mediator.Send(new GetTeacherWithdrawalsQuery(teacherId, pageNumber, pageSize), cancellationToken);
        return Ok(result);
    }

    [HttpGet("payout-accounts")]
    [ProducesResponseType(typeof(List<TeacherPayoutAccountDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<TeacherPayoutAccountDto>>> GetPayoutAccounts(CancellationToken cancellationToken)
    {
        var teacherId = GetTeacherId();
        var result = await _mediator.Send(new GetTeacherPayoutAccountsQuery(teacherId), cancellationToken);
        return Ok(result);
    }

    [HttpPost("payout-accounts")]
    [ProducesResponseType(typeof(TeacherPayoutAccountDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TeacherPayoutAccountDto>> CreatePayoutAccount(
        [FromBody] CreatePayoutAccountRequest request,
        CancellationToken cancellationToken)
    {
        var teacherId = GetTeacherId();
        var command = new CreatePayoutAccountCommand(teacherId, request.AccountType, request.AccountName, request.AccountIdentifier, request.IsDefault);
        var result = await _mediator.Send(command, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPut("payout-accounts/{id:guid}")]
    [ProducesResponseType(typeof(TeacherPayoutAccountDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TeacherPayoutAccountDto>> UpdatePayoutAccount(
        Guid id,
        [FromBody] UpdatePayoutAccountRequest request,
        CancellationToken cancellationToken)
    {
        var teacherId = GetTeacherId();
        var command = new UpdatePayoutAccountCommand(id, teacherId, request.AccountType, request.AccountName, request.AccountIdentifier, request.IsDefault);
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("payout-accounts/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePayoutAccount(Guid id, CancellationToken cancellationToken)
    {
        var teacherId = GetTeacherId();
        await _mediator.Send(new DeletePayoutAccountCommand(id, teacherId), cancellationToken);
        return NoContent();
    }
}

public record InitiateTopUpRequest(decimal Amount);
public record RequestWithdrawalRequest(decimal Amount);
public record CreatePayoutAccountRequest(PayoutAccountType AccountType, string AccountName, string AccountIdentifier, bool IsDefault = false);
public record UpdatePayoutAccountRequest(PayoutAccountType AccountType, string AccountName, string AccountIdentifier, bool IsDefault = false);
