using Draya.Application.Exams.Services;
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

    public ExamsController(IExamGenerationService examGenerationService)
    {
        _examGenerationService = examGenerationService;
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
}
