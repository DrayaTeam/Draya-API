using Draya.Application.Exams.DTOs;
using Draya.Application.Exams.Queries.GetExamById;
using Draya.Application.Exams.Queries.GetTeacherAIExamQuota;
using Draya.Application.Exams.Services;
using Draya.Domain.Exams;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace Draya.Api.Controllers;

[ApiController]
[Route("api/v1/exams")]
[Authorize]
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
    [Authorize(Roles = "Teacher")]
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
        
        // Ensure that teachers cannot accidentally generate practice exams 
        // (which are hidden from their dashboard and bypass quotas)
        request.IsPracticeReview = false;

        // Start the background generation task
        var generationId = await _examGenerationService.StartGenerationAsync(request, cancellationToken);
        
        // Return 202 Accepted with the generation ID
        return Accepted(new { 
            GenerationId = generationId, 
            Message = "Exam generation has started in the background. Connect to the SignalR hub '/hubs/exam-generation' to receive progress updates." 
        });
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Teacher,Student")]
    [ProducesResponseType(typeof(ExamDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetExamById(Guid id)
    {
        var isStudent = User.IsInRole("Student");

        if (isStudent)
        {
            var studentIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(studentIdStr, out var studentId)) return Unauthorized();

            var studentExam = await _mediator.Send(new Draya.Application.Exams.Queries.GetStudentExamById.GetStudentExamByIdQuery(id, studentId));
            if (studentExam == null)
                return NotFound(new { message = $"Exam with ID {id} not found." });
            return Ok(studentExam);
        }

        var exam = await _mediator.Send(new GetExamByIdQuery(id));
        
        if (exam == null)
            return NotFound(new { message = $"Exam with ID {id} not found." });
            
        return Ok(exam);
    }

    [HttpGet("generations/{generationId}")]
    [Authorize(Roles = "Teacher,Student")]
    [ProducesResponseType(typeof(ExamGenerationStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExamGenerationStatusDto), StatusCodes.Status206PartialContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetGenerationStatus(
        [FromRoute] Guid generationId,
        [FromServices] Draya.Domain.Exams.IExamGenerationRepository generationRepo,
        CancellationToken cancellationToken)
    {
        var generation = await generationRepo.GetByIdAsync(generationId, cancellationToken);
        if (generation == null)
            return NotFound();

        var body = new ExamGenerationStatusDto
        {
            Id = generation.Id,
            Status = generation.Status,
            StatusName = generation.Status.ToString(),
            RequestedCount = generation.RequestedCount,
            GeneratedCount = generation.GeneratedCount,
            CreatedAt = generation.CreatedAt,
            CompletedAt = generation.CompletedAt,
            ErrorMessage = generation.ErrorMessage,
            ExamId = generation.ExamId
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

            // Topic not covered in material — return 200 OK so frontend can read the DataUnavailable status
            Draya.Domain.Exams.GenerationStatus.DataUnavailable => Ok(body),

            // Unexpected system failure - return 200 OK so frontend can read the failed status
            Draya.Domain.Exams.GenerationStatus.Failed => Ok(body),

            _ => Ok(body)
        };
    }

    [HttpGet]
    [Authorize(Roles = "Teacher")]
    [ProducesResponseType(typeof(Draya.Application.Classrooms.DTOs.PagedResult<ExamSummaryDto>), StatusCodes.Status200OK)]
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
            .Where(e => e.ClassroomId == classroomId && !e.Title.StartsWith("Practice Mini-Exam:"))
            .OrderByDescending(e => e.CreatedAt);

        var total = query.Count();
        var exams = query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new ExamSummaryDto
            {
                Id = e.Id,
                Title = e.Title,
                Topic = e.Topic,
                DurationMinutes = e.DurationMinutes,
                StartDate = e.StartDate,
                EndDate = e.EndDate,
                AllowedAttempts = e.AllowedAttempts,
                CreatedAt = e.CreatedAt,
                QuestionsCount = e.Questions.Count
            })
            .ToList();

        var totalPages = (int)Math.Ceiling((double)total / pageSize);
        return Ok(new Draya.Application.Classrooms.DTOs.PagedResult<ExamSummaryDto>(exams, page, pageSize, total, totalPages));
    }

    [HttpGet("{examId:guid}/attempts")]
    [Authorize(Roles = "Teacher")]
    [ProducesResponseType(typeof(ExamAttemptsResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetExamAttempts(
        [FromRoute] Guid examId,
        [FromServices] Draya.Infrastructure.Persistence.ApplicationDbContext dbContext,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var teacherIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(teacherIdStr, out var teacherId)) return Unauthorized();

        // Ensure teacher owns the classroom
        var exam = await dbContext.Exams.FirstOrDefaultAsync(e => e.Id == examId, cancellationToken);
            
        if (exam == null) return NotFound();
        var hasAccess = await dbContext.Classrooms.AnyAsync(c => c.Id == exam.ClassroomId && c.TeacherId == teacherId, cancellationToken);
        if (!hasAccess) return Forbid();

        var query = dbContext.StudentExamAttempts
            .Join(dbContext.Students, a => a.StudentId, s => s.UserId, (a, s) => new { Attempt = a, Student = s })
            .Where(x => x.Attempt.ExamId == examId && x.Attempt.IsSubmitted)
            .OrderByDescending(x => x.Attempt.SubmittedAt);

        var total = await query.CountAsync(cancellationToken);
        var attempts = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new ExamAttemptSummaryDto
            {
                Id = x.Attempt.Id,
                StudentId = x.Attempt.StudentId,
                StudentName = x.Student != null ? x.Student.FullName : "Unknown",
                FinalScore = x.Attempt.FinalScore,
                MaxScore = x.Attempt.MaxScore,
                SubmittedAt = x.Attempt.SubmittedAt,
                NeedsTeacherReview = x.Attempt.NeedsTeacherReview
            })
            .ToListAsync(cancellationToken);

        return Ok(new ExamAttemptsResponseDto { Items = attempts, TotalCount = total });
    }

    [HttpPost("{examId:guid}/questions")]
    [Authorize(Roles = "Teacher")]
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
    [Authorize(Roles = "Teacher")]
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

    [HttpGet("{examId:guid}/student-view")]
    [Authorize(Roles = "Student")]
    [ProducesResponseType(typeof(StudentExamViewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStudentExamView(
        [FromRoute] Guid examId,
        [FromServices] Draya.Infrastructure.Persistence.ApplicationDbContext dbContext,
        CancellationToken cancellationToken = default)
    {
        var studentIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(studentIdStr, out var studentId)) return Unauthorized();

        var exam = await dbContext.Exams
            .Select(e => new ExamSummaryDto
            {
                Id = e.Id,
                Title = e.Title,
                Topic = e.Topic,
                DurationMinutes = e.DurationMinutes,
                StartDate = e.StartDate,
                EndDate = e.EndDate,
                AllowedAttempts = e.AllowedAttempts,
                CreatedAt = e.CreatedAt,
                QuestionsCount = e.Questions.Count
            })
            .FirstOrDefaultAsync(e => e.Id == examId, cancellationToken);

        if (exam == null) return NotFound();

        if (exam.Title.StartsWith("Practice Mini-Exam:"))
        {
            var isMine = await dbContext.ExamGenerations.AnyAsync(g => g.ExamId == examId && g.IdempotencyKey.StartsWith($"practice_{studentId}_"), cancellationToken);
            if (!isMine) return NotFound();
        }

        var attempts = await dbContext.StudentExamAttempts
            .Where(a => a.ExamId == examId && a.StudentId == studentId)
            .OrderByDescending(a => a.SubmittedAt)
            .Select(a => new StudentExamAttemptDto
            {
                Id = a.Id,
                IsSubmitted = a.IsSubmitted,
                SubmittedAt = a.SubmittedAt,
                FinalScore = a.FinalScore,
                MaxScore = a.MaxScore,
                NeedsTeacherReview = a.NeedsTeacherReview
            })
            .ToListAsync(cancellationToken);

        return Ok(new StudentExamViewDto
        {
            Exam = exam,
            Attempts = attempts
        });
    }

    [HttpDelete("{examId:guid}/questions/{questionId:guid}")]
    [Authorize(Roles = "Teacher")]
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
    [Authorize(Roles = "Teacher")]
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

    [HttpGet("quota")]
    [Authorize(Roles = "Teacher")]
    [ProducesResponseType(typeof(AIExamQuotaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetTeacherAIExamQuota(CancellationToken cancellationToken)
    {
        var teacherIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(teacherIdStr, out var teacherId))
        {
            return Unauthorized();
        }

        var result = await _mediator.Send(new GetTeacherAIExamQuotaQuery(teacherId), cancellationToken);
        return Ok(result);
    }
}

public class ExamSummaryDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int AllowedAttempts { get; set; }
    public DateTime CreatedAt { get; set; }
    public int QuestionsCount { get; set; }
}

public class ExamGenerationStatusDto
{
    public Guid Id { get; set; }
    public Draya.Domain.Exams.GenerationStatus Status { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public int RequestedCount { get; set; }
    public int GeneratedCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public Guid? ExamId { get; set; }
}

public class ExamAttemptSummaryDto
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public decimal? FinalScore { get; set; }
    public decimal? MaxScore { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public bool NeedsTeacherReview { get; set; }
}

public class ExamAttemptsResponseDto
{
    public List<ExamAttemptSummaryDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
}

public class StudentExamViewDto
{
    public ExamSummaryDto Exam { get; set; } = null!;
    public List<StudentExamAttemptDto> Attempts { get; set; } = new();
}

public class StudentExamAttemptDto
{
    public Guid Id { get; set; }
    public bool IsSubmitted { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public decimal? FinalScore { get; set; }
    public decimal? MaxScore { get; set; }
    public bool NeedsTeacherReview { get; set; }
}
