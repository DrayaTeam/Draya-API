using System;
using System.Threading;
using System.Threading.Tasks;
using Draya.Application.Reports.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Draya.Api.Controllers.v1;

[ApiController]
[Route("api/v1/students/{studentId}/weak-topics/{topicName}")]
[Authorize(Roles = "Student")]
public class InteractiveReviewController : ControllerBase
{
    private readonly IInteractiveReviewService _reviewService;

    public InteractiveReviewController(IInteractiveReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    /// <summary>
    /// Gets a revision summary for a weak topic, including recommendations and source materials.
    /// </summary>
    [HttpGet("revision")]
    [ProducesResponseType(typeof(TopicRevisionDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRevision(Guid studentId, string topicName, CancellationToken cancellationToken)
    {
        var loggedInId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (loggedInId != studentId.ToString())
            return Forbid();

        var revision = await _reviewService.GetRevisionAsync(studentId, topicName, cancellationToken);
        return Ok(revision);
    }

    /// <summary>
    /// Generates a practice mini-exam for a weak topic.
    /// </summary>
    [HttpPost("practice-exam")]
    [ProducesResponseType(typeof(PracticeExamResponseDto), StatusCodes.Status202Accepted)]
    public async Task<IActionResult> GeneratePracticeExam(
        Guid studentId, 
        string topicName, 
        [FromBody] PracticeExamRequest request,
        CancellationToken cancellationToken)
    {
        var loggedInId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (loggedInId != studentId.ToString())
            return Forbid();

        var jobId = await _reviewService.GeneratePracticeExamAsync(studentId, topicName, request, cancellationToken);

        return Accepted(new PracticeExamResponseDto 
        { 
            GenerationId = jobId,
            Message = "Practice exam generation has started in the background. Connect to the SignalR hub '/hubs/exam-generation' to receive progress updates." 
        });
    }
}

public class PracticeExamResponseDto
{
    public Guid GenerationId { get; set; }
    public string Message { get; set; } = string.Empty;
}
