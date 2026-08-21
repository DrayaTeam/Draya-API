using Draya.Application.Exams.DTOs;
using Draya.Application.Exams.Queries.GetExamById;
using Draya.Application.Exams.Services;
using Draya.Domain.Exams;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Draya.Api.Controllers;

[ApiController]
[Route("api/v1/exams")]
[Authorize(Roles = "Teacher")]
[Produces("application/json")]
public class ExamsController : ControllerBase
{
    private readonly IExamGenerationService _examGenerationService;
    private readonly IMediator _mediator;

    public ExamsController(IExamGenerationService examGenerationService, IMediator mediator)
    {
        _examGenerationService = examGenerationService;
        _mediator = mediator;
    }

    [HttpPost("generate")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> GenerateExam(
        [FromBody] GenerateExamRequest request,
        CancellationToken cancellationToken)
    {
        var teacherIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(teacherIdStr, out var teacherId))
        {
            return Unauthorized();
        }

        // Set the teacher ID from the authenticated user
        request.TeacherId = teacherId;

        // Start the background generation task
        var generationId = await _examGenerationService.StartGenerationAsync(request, cancellationToken);
        
        // Return 202 Accepted with the generation ID
        return Accepted(new { 
            GenerationId = generationId, 
            Message = "Exam generation has started in the background. Connect to the SignalR hub '/hubs/exam-generation' to receive progress updates." 
        });
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ExamDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetExamById(Guid id)
    {
        var exam = await _mediator.Send(new GetExamByIdQuery(id));
        
        if (exam == null)
            return NotFound(new { message = $"Exam with ID {id} not found." });
            
        return Ok(exam);
    }

    [HttpGet("generations/{generationId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status206PartialContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetGenerationStatus(
        [FromRoute] Guid generationId,
        [FromServices] Draya.Domain.Exams.IExamGenerationRepository generationRepo,
        CancellationToken cancellationToken)
    {
        var generation = await generationRepo.GetByIdAsync(generationId, cancellationToken);
        if (generation == null)
            return NotFound();

        var body = new
        {
            generation.Id,
            generation.Status,
            StatusName = generation.Status.ToString(),
            generation.RequestedCount,
            generation.GeneratedCount,
            generation.CreatedAt,
            generation.CompletedAt,
            generation.ErrorMessage,
            generation.ExamId
        };

        return generation.Status switch
        {
            // Still running — report progress with 200 (client polls again)
            Draya.Domain.Exams.GenerationStatus.Pending
                or Draya.Domain.Exams.GenerationStatus.Retrieving
                or Draya.Domain.Exams.GenerationStatus.Generating
                or Draya.Domain.Exams.GenerationStatus.Validating => Ok(body),

            // Full success
            Draya.Domain.Exams.GenerationStatus.Completed => Ok(body),

            // Partial success — exam created but fewer questions than requested
            Draya.Domain.Exams.GenerationStatus.CompletedWithWarning => StatusCode(StatusCodes.Status206PartialContent, body),

            // Topic not covered in material — no exam created
            Draya.Domain.Exams.GenerationStatus.DataUnavailable => UnprocessableEntity(body),

            // Unexpected system failure
            Draya.Domain.Exams.GenerationStatus.Failed => StatusCode(StatusCodes.Status500InternalServerError, body),

            _ => Ok(body)
        };
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetExams(
        [FromQuery] Guid classroomId,
        [FromServices] Draya.Infrastructure.Persistence.ApplicationDbContext dbContext,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var teacherIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(teacherIdStr, out var teacherId))
        {
            return Unauthorized();
        }

        var query = dbContext.Exams
            .Where(e => e.ClassroomId == classroomId)
            .OrderByDescending(e => e.CreatedAt);

        var total = query.Count();
        var exams = query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new
            {
                e.Id,
                e.Title,
                e.Topic,
                e.DurationMinutes,
                e.StartDate,
                e.EndDate,
                e.AllowedAttempts,
                e.CreatedAt,
                QuestionsCount = e.Questions.Count
            })
            .ToList();

        return Ok(new { items = exams, totalCount = total });
    }

    [HttpPost("{examId:guid}/questions")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddQuestion(Guid examId, [FromBody] AddExamQuestionRequest request, CancellationToken cancellationToken)
    {
        var teacherIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(teacherIdStr, out var teacherId)) return Unauthorized();

        var cmd = new Draya.Application.Exams.Commands.Questions.AddExamQuestionCommand
        {
            ExamId = examId,
            TeacherId = teacherId,
            Text = request.Text,
            Type = request.Type,
            Difficulty = request.Difficulty,
            Rubric = request.Rubric,
            SourceChunkIds = request.SourceChunkIds,
            Options = request.Options
        };

        var result = await _mediator.Send(cmd, cancellationToken);
        if (result == null) return NotFound();

        return Ok(new { QuestionId = result });
    }

    [HttpPut("{examId:guid}/questions/{questionId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateQuestion(Guid examId, Guid questionId, [FromBody] UpdateExamQuestionRequest request, CancellationToken cancellationToken)
    {
        var teacherIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(teacherIdStr, out var teacherId)) return Unauthorized();

        var cmd = new Draya.Application.Exams.Commands.Questions.UpdateExamQuestionCommand
        {
            ExamId = examId,
            QuestionId = questionId,
            TeacherId = teacherId,
            Text = request.Text,
            Type = request.Type,
            Difficulty = request.Difficulty,
            Rubric = request.Rubric,
            Options = request.Options
        };

        var result = await _mediator.Send(cmd, cancellationToken);
        if (!result) return NotFound();

        return NoContent();
    }

    [HttpDelete("{examId:guid}/questions/{questionId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteQuestion(Guid examId, Guid questionId, CancellationToken cancellationToken)
    {
        var teacherIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(teacherIdStr, out var teacherId)) return Unauthorized();

        var cmd = new Draya.Application.Exams.Commands.Questions.DeleteExamQuestionCommand
        {
            ExamId = examId,
            QuestionId = questionId,
            TeacherId = teacherId
        };

        var result = await _mediator.Send(cmd, cancellationToken);
        if (!result) return NotFound();

        return NoContent();
    }

    [HttpPost("{examId:guid}/questions/{questionId:guid}/refine")]
    [ProducesResponseType(typeof(Draya.Application.Exams.Services.GeneratedQuestionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RefineQuestion(Guid examId, Guid questionId, [FromBody] RefineExamQuestionRequest request, CancellationToken cancellationToken)
    {
        var teacherIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(teacherIdStr, out var teacherId)) return Unauthorized();

        var cmd = new Draya.Application.Exams.Commands.Questions.RefineExamQuestionCommand
        {
            ExamId = examId,
            QuestionId = questionId,
            TeacherId = teacherId,
            Instruction = request.Instruction
        };

        var result = await _mediator.Send(cmd, cancellationToken);
        if (result == null) return BadRequest(new { message = "Could not generate a refined question based on the provided instruction and context." });

        return Ok(result);
    }
}
