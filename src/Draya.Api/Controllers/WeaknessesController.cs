using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Draya.Infrastructure.Persistence;
using Draya.Application.Reports.DTOs;

namespace Draya.Api.Controllers;

[ApiController]
[Route("api/v1/weaknesses")]
public class WeaknessesController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;

    public WeaknessesController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("active")]
    [Authorize(Roles = "Student")]
    [ProducesResponseType(typeof(System.Collections.Generic.List<WeaknessDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActiveWeaknesses(CancellationToken cancellationToken)
    {
        var studentIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(studentIdStr, out var studentId)) return Unauthorized();

        var weaknesses = await _dbContext.StudentWeaknesses
            .Where(w => w.StudentId == studentId && w.IsActive)
            .Select(w => new WeaknessDto(
                w.Id,
                w.TopicId,
                w.TopicNameSnapshot,
                w.CurrentProficiencyPercent,
                w.LastUpdatedAt
            ))
            .ToListAsync(cancellationToken);

        return Ok(weaknesses);
    }

    [HttpGet("resolved")]
    [Authorize(Roles = "Student")]
    [ProducesResponseType(typeof(System.Collections.Generic.List<ResolvedWeaknessDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetResolvedWeaknesses(CancellationToken cancellationToken)
    {
        var studentIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(studentIdStr, out var studentId)) return Unauthorized();

        var weaknesses = await _dbContext.StudentWeaknesses
            .Where(w => w.StudentId == studentId && !w.IsActive)
            .Select(w => new
            {
                w.Id,
                w.TopicId,
                TopicName = w.TopicNameSnapshot,
                w.CurrentProficiencyPercent,
                w.LastUpdatedAt
            })
            .ToListAsync(cancellationToken);

        // Fetch deltas
        var result = new System.Collections.Generic.List<ResolvedWeaknessDto>();
        foreach (var w in weaknesses)
        {
            var histories = await _dbContext.StudentWeaknessHistories
                .Where(h => h.StudentWeaknessId == w.Id)
                .OrderByDescending(h => h.CreatedAt)
                .Take(2)
                .ToListAsync(cancellationToken);
            
            decimal previous = w.CurrentProficiencyPercent;
            if (histories.Count > 1) previous = histories[1].NewProficiencyPercent;
            else if (histories.Count == 1) previous = histories[0].PreviousProficiencyPercent;

            result.Add(new ResolvedWeaknessDto(
                w.Id,
                w.TopicId,
                w.TopicName,
                w.CurrentProficiencyPercent,
                previous,
                w.CurrentProficiencyPercent - previous,
                w.LastUpdatedAt
            ));
        }

        return Ok(result);
    }

    [HttpGet("{id:guid}/history")]
    [Authorize(Roles = "Student")]
    [ProducesResponseType(typeof(System.Collections.Generic.List<WeaknessHistoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetWeaknessHistory(Guid id, CancellationToken cancellationToken)
    {
        var studentIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(studentIdStr, out var studentId)) return Unauthorized();

        var weakness = await _dbContext.StudentWeaknesses.FirstOrDefaultAsync(w => w.Id == id, cancellationToken);
        if (weakness == null) return NotFound();
        if (weakness.StudentId != studentId) return Forbid();

        var histories = await _dbContext.StudentWeaknessHistories
            .Where(h => h.StudentWeaknessId == id)
            .OrderBy(h => h.CreatedAt)
            .Select(h => new WeaknessHistoryDto(
                h.Id,
                h.PreviousProficiencyPercent,
                h.NewProficiencyPercent,
                h.PreviousIsActive,
                h.NewIsActive,
                h.CreatedAt,
                h.SourceAttemptId
            ))
            .ToListAsync(cancellationToken);

        return Ok(histories);
    }
}
