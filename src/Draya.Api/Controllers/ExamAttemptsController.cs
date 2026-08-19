using Draya.Application.Exams.Services;
using Draya.Domain.Exams;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace Draya.Api.Controllers;

[ApiController]
[Route("api/v1/attempts")]
[Produces("application/json")]
public class ExamAttemptsController : ControllerBase
{
    private readonly IExamGradingService _examGradingService;
    private readonly IStudentExamAttemptRepository _attemptRepo;
    private readonly IExamGradingJobRepository _jobRepo;
    private readonly IMediator _mediator;

    public ExamAttemptsController(
        IExamGradingService examGradingService,
        IStudentExamAttemptRepository attemptRepo,
        IExamGradingJobRepository jobRepo,
        IMediator mediator)
    {
        _examGradingService = examGradingService;
        _attemptRepo = attemptRepo;
        _jobRepo = jobRepo;
        _mediator = mediator;
    }

    [HttpPost("start")]
    [Authorize(Roles = "Student")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> StartAttempt([FromBody] StartAttemptRequestDto request, CancellationToken cancellationToken)
    {
        var studentIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(studentIdClaim, out var studentId))
        {
            return Unauthorized();
        }

        var command = new Draya.Application.Exams.Commands.Attempts.StartExamAttemptCommand(request.ExamId, studentId);
        var attemptId = await _mediator.Send(command, cancellationToken);
        
        return Ok(new { AttemptId = attemptId });
    }

    [HttpPost("{attemptId:guid}/submit")]
    [Authorize(Roles = "Student")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SubmitAttempt(Guid attemptId, [FromBody] SubmitAttemptRequestDto request, CancellationToken cancellationToken)
    {
        var command = new Draya.Application.Exams.Commands.Attempts.SubmitExamAttemptCommand(attemptId, request.Answers, request.IdempotencyKey);
        var jobId = await _mediator.Send(command, cancellationToken);
        
        return Accepted(new { GradingJobId = jobId, Message = "Exam submitted successfully. Grading has started. Connect to SignalR hub." });
    }

    [HttpPost("{attemptId:guid}/grade")]
    [Authorize(Roles = "Teacher,Student")] // Maybe only System/Student triggers this normally, or Teacher manually
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> StartGrading(Guid attemptId, [FromBody] StartGradingRequestDto requestDto, CancellationToken cancellationToken)
    {
        var attempt = await _attemptRepo.GetByIdAsync(attemptId, cancellationToken);
        if (attempt == null) return NotFound("Attempt not found");

        var request = new StartGradingRequest
        {
            StudentExamAttemptId = attemptId,
            IdempotencyKey = requestDto.IdempotencyKey
        };

        var jobId = await _examGradingService.StartGradingAsync(request, cancellationToken);
        
        return Accepted(new { GradingJobId = jobId, Message = "Grading has started. Connect to SignalR hub." });
    }

    [HttpGet("jobs/{jobId:guid}")]
    [Authorize(Roles = "Teacher,Student")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetJobStatus(Guid jobId, CancellationToken cancellationToken)
    {
        var job = await _jobRepo.GetByIdAsync(jobId, cancellationToken);
        if (job == null) return NotFound();

        return Ok(new
        {
            job.Id,
            job.StudentExamAttemptId,
            Status = job.Status.ToString(),
            job.CreatedAt,
            job.CompletedAt,
            job.ErrorMessage
        });
    }

    [HttpPut("{attemptId:guid}/answers/{answerId:guid}/override")]
    [Authorize(Roles = "Teacher")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> OverrideAnswerScore(Guid attemptId, Guid answerId, [FromBody] OverrideScoreRequestDto request, CancellationToken cancellationToken)
    {
        var attempt = await _attemptRepo.GetByIdAsync(attemptId, cancellationToken);
        if (attempt == null) return NotFound("Attempt not found");

        var answer = attempt.Answers.FirstOrDefault(a => a.Id == answerId);
        if (answer == null || answer.GradingResult == null) return NotFound("Answer or grading result not found");

        answer.GradingResult.OverrideScore(request.NewScore);

        // Recalculate exam total score
        decimal total = 0;
        bool stillNeedsReview = false;
        foreach (var a in attempt.Answers)
        {
            if (a.GradingResult != null)
            {
                total += a.GradingResult.GetFinalScore();
                if (a.GradingResult.NeedsTeacherReview)
                    stillNeedsReview = true;
            }
        }

        attempt.UpdateFinalScore(total, stillNeedsReview);
        await _attemptRepo.SaveGradingResultsAsync(attempt, new System.Collections.Generic.List<Draya.Domain.Exams.AnswerGradingResult>(), cancellationToken);

        return NoContent();
    }

    [HttpGet("{attemptId:guid}/results")]
    [Authorize(Roles = "Teacher,Student")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAttemptResults(Guid attemptId, CancellationToken cancellationToken)
    {
        var attempt = await _attemptRepo.GetByIdAsync(attemptId, cancellationToken);
        if (attempt == null) return NotFound("Attempt not found");

        var answers = attempt.Answers.Select(a => new
        {
            AnswerId = a.Id,
            a.ExamQuestionId,
            a.AnswerText,
            a.SelectedOptionId,
            GradingResult = a.GradingResult == null ? null : new
            {
                Score = a.GradingResult.GetFinalScore(),
                a.GradingResult.MaxScore,
                a.GradingResult.ConfidenceScore,
                a.GradingResult.IsAiGraded,
                a.GradingResult.NeedsTeacherReview,
                a.GradingResult.Rationale,
                a.GradingResult.TeacherOverrideScore
            }
        });

        return Ok(new
        {
            AttemptId = attempt.Id,
            attempt.ExamId,
            attempt.IsSubmitted,
            attempt.SubmittedAt,
            attempt.FinalScore,
            attempt.NeedsTeacherReview,
            Answers = answers
        });
    }
}

public class StartGradingRequestDto
{
    public string IdempotencyKey { get; set; } = string.Empty;
}

public class OverrideScoreRequestDto
{
    public decimal NewScore { get; set; }
}

public class StartAttemptRequestDto
{
    public Guid ExamId { get; set; }
}

public class SubmitAttemptRequestDto
{
    public List<Draya.Application.Exams.Commands.Attempts.AnswerSubmissionDto> Answers { get; set; } = new();
    public string IdempotencyKey { get; set; } = string.Empty;
}
