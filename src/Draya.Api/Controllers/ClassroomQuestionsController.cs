using Draya.Application.Common.Models;
using Draya.Application.Classrooms.Questions.Commands;
using Draya.Application.Classrooms.Questions.DTOs;
using Draya.Application.Classrooms.Questions.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Draya.Api.Controllers;

[ApiController]
[Route("api/v1/classrooms/{classroomId}/questions")]
[Authorize]
[Produces("application/json")]
public class ClassroomQuestionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ClassroomQuestionsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResult<QuestionDto>>> GetQuestions(
        Guid classroomId,
        [FromQuery] string sortBy = "recent",
        [FromQuery] string filterBy = "all",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new GetClassroomQuestionsQuery(
            classroomId,
            GetUserId(),
            GetUserRole(),
            sortBy,
            filterBy,
            page,
            pageSize
        );
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{questionId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<QuestionDetailsDto>> GetQuestionDetails(
        Guid classroomId,
        Guid questionId,
        CancellationToken cancellationToken)
    {
        var query = new GetQuestionDetailsQuery(questionId, GetUserId());
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<QuestionDto>> CreateQuestion(
        Guid classroomId,
        [FromBody] CreateQuestionRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateQuestionCommand(classroomId, GetUserId(), request.Content, null, null, null, request.ImageUrl);
        var result = await _mediator.Send(command, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPost("with-photo")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<QuestionDto>> CreateQuestionWithPhoto(
        Guid classroomId,
        [FromForm] string content,
        IFormFile? file,
        CancellationToken cancellationToken)
    {
        Stream? stream = file != null && file.Length > 0 ? file.OpenReadStream() : null;
        var command = new CreateQuestionCommand(
            classroomId,
            GetUserId(),
            content,
            stream,
            file?.FileName,
            file?.ContentType);

        var result = await _mediator.Send(command, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPost("{questionId}/replies")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<QuestionReplyDto>> CreateReply(
        Guid classroomId,
        Guid questionId,
        [FromBody] CreateQuestionReplyRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateQuestionReplyCommand(questionId, GetUserId(), request.Content, null, null, null, request.ImageUrl);
        var result = await _mediator.Send(command, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPost("{questionId}/replies/with-photo")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<QuestionReplyDto>> CreateReplyWithPhoto(
        Guid classroomId,
        Guid questionId,
        [FromForm] string content,
        IFormFile? file,
        CancellationToken cancellationToken)
    {
        Stream? stream = file != null && file.Length > 0 ? file.OpenReadStream() : null;
        var command = new CreateQuestionReplyCommand(
            questionId,
            GetUserId(),
            content,
            stream,
            file?.FileName,
            file?.ContentType);

        var result = await _mediator.Send(command, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }


    [HttpPost("{questionId}/vote")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> VoteQuestion(
        Guid classroomId,
        Guid questionId,
        CancellationToken cancellationToken)
    {
        var command = new VoteQuestionCommand(questionId, GetUserId());
        await _mediator.Send(command, cancellationToken);
        return Ok();
    }

    [HttpDelete("{questionId}/vote")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemoveVoteQuestion(
        Guid classroomId,
        Guid questionId,
        CancellationToken cancellationToken)
    {
        var command = new RemoveVoteCommand(questionId, GetUserId());
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpPut("{questionId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> EditQuestion(
        Guid classroomId,
        Guid questionId,
        [FromBody] EditQuestionRequest request,
        CancellationToken cancellationToken)
    {
        var command = new EditQuestionCommand(questionId, GetUserId(), request.Content);
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{questionId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteQuestion(
        Guid classroomId,
        Guid questionId,
        CancellationToken cancellationToken)
    {
        var command = new DeleteQuestionCommand(questionId, GetUserId());
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpPut("{questionId}/replies/{replyId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> EditReply(
        Guid classroomId,
        Guid questionId,
        Guid replyId,
        [FromBody] EditQuestionReplyRequest request,
        CancellationToken cancellationToken)
    {
        var command = new EditQuestionReplyCommand(replyId, GetUserId(), request.Content);
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{questionId}/replies/{replyId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteReply(
        Guid classroomId,
        Guid questionId,
        Guid replyId,
        CancellationToken cancellationToken)
    {
        var command = new DeleteQuestionReplyCommand(replyId, GetUserId());
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);

        if (!Guid.TryParse(userIdClaim, out var userId))
            throw new UnauthorizedAccessException();

        return userId;
    }

    private string GetUserRole()
    {
        return User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
    }
}

public record CreateQuestionRequest(string Content);
public record CreateQuestionReplyRequest(string Content);
public record EditQuestionRequest(string Content);
public record EditQuestionReplyRequest(string Content);
