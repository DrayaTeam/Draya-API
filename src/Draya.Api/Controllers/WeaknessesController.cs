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

namespace Draya.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class WeaknessesController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;

    public WeaknessesController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("active")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> GetActiveWeaknesses(CancellationToken cancellationToken)
    {
        var studentIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(studentIdStr, out var studentId)) return Unauthorized();

        var weaknesses = await _dbContext.StudentWeaknesses
            .Where(w => w.StudentId == studentId && w.IsActive)
            .Select(w => new
            {
                w.Id,
                w.TopicId,
                TopicName = w.TopicNameSnapshot,
                w.CurrentProficiencyPercent,
                w.LastUpdatedAt
            })
            .ToListAsync(cancellationToken);

        return Ok(weaknesses);
    }

    [HttpGet("resolved")]
    [Authorize(Roles = "Student")]
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
        var result = new System.Collections.Generic.List<object>();
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

            result.Add(new
            {
                w.Id,
                w.TopicId,
                w.TopicName,
                w.CurrentProficiencyPercent,
                PreviousProficiencyPercent = previous,
                Delta = w.CurrentProficiencyPercent - previous,
                w.LastUpdatedAt
            });
        }

        return Ok(result);
    }
}
