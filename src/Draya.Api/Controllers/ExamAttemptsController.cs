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
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SubmitAttempt(Guid attemptId, [FromBody] SubmitAttemptRequestDto request, CancellationToken cancellationToken)
    {
        var command = new Draya.Application.Exams.Commands.Attempts.SubmitExamAttemptCommand(attemptId, request.Answers, request.IdempotencyKey);
        var jobId = await _mediator.Send(command, cancellationToken);
        
        if (jobId == null)
        {
            return Ok(new { Message = "Exam submitted and auto-graded successfully.", AttemptId = attemptId });
        }

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

        var exam = await _mediator.Send(new Draya.Application.Exams.Queries.GetExamById.GetExamByIdQuery(attempt.ExamId), cancellationToken);
        var questions = exam?.Questions;

        var answers = attempt.Answers?.Select(a => 
        {
            var question = questions?.FirstOrDefault(q => q.Id == a.ExamQuestionId);
            var correctOption = question?.Options?.FirstOrDefault(o => o.IsCorrect);
            
            return new
            {
                AnswerId = a.Id,
                ExamQuestionId = a.ExamQuestionId,
                AnswerText = a.AnswerText ?? string.Empty,
                SelectedOptionId = a.SelectedOptionId,
                CorrectOptionId = correctOption?.Id,
                CorrectAnswerText = correctOption?.Text,
                GradingResult = a.GradingResult == null ? null : new
                {
                    Score = a.GradingResult.GetFinalScore(),
                    MaxScore = Math.Max(a.GradingResult.MaxScore, 1.0m), // Prevent divide-by-zero
                    ConfidenceScore = a.GradingResult.ConfidenceScore,
                    IsAiGraded = a.GradingResult.IsAiGraded,
                    NeedsTeacherReview = a.GradingResult.NeedsTeacherReview,
                    Rationale = a.GradingResult.Rationale ?? "No rationale provided.",
                    TeacherOverrideScore = a.GradingResult.TeacherOverrideScore
                }
            };
        }) ?? Enumerable.Empty<object>();

        return Ok(new
        {
            AttemptId = attempt.Id,
            ExamId = attempt.ExamId,
            IsSubmitted = attempt.IsSubmitted,
            SubmittedAt = attempt.SubmittedAt,
            FinalScore = attempt.FinalScore ?? 0m,
            NeedsTeacherReview = attempt.NeedsTeacherReview,
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
