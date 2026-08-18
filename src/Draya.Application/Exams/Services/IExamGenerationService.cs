using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Draya.Application.Exams.Services;

public class GenerateExamRequest
{
    public Guid TeacherId { get; set; }
    public Guid ClassroomId { get; set; }
    public Guid MaterialVersionId { get; set; }
    public string Topic { get; set; } = string.Empty;
    public string DifficultyLevel { get; set; } = "Medium";
    public int RequestedCount { get; set; }
    public string TeacherInstructions { get; set; } = string.Empty;
    public string IdempotencyKey { get; set; } = string.Empty;
}

public interface IExamGenerationService
{
    Task<Guid> StartGenerationAsync(GenerateExamRequest request, CancellationToken cancellationToken = default);
    Task ProcessGenerationAsync(Guid generationId, GenerateExamRequest request, CancellationToken cancellationToken = default);
}
