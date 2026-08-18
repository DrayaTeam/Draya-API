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
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetGenerationStatus(
        [FromRoute] Guid generationId,
        [FromServices] Draya.Domain.Exams.IExamGenerationRepository generationRepo,
        CancellationToken cancellationToken)
    {
        var generation = await generationRepo.GetByIdAsync(generationId, cancellationToken);
        if (generation == null)
        {
            return NotFound();
        }

        return Ok(new
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
        });
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
                e.CreatedAt,
                QuestionsCount = e.Questions.Count
            })
            .ToList();

        return Ok(new { items = exams, totalCount = total });
    }
}
