using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Draya.Application.Exams.Services;

public class QuestionTypeRequirement
{
    public string Type { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class GenerateExamRequest
{
    public Guid TeacherId { get; set; }
    public Guid ClassroomId { get; set; }
    public Guid SectionId { get; set; }
    public string Topic { get; set; } = string.Empty;
    public string DifficultyLevel { get; set; } = "Medium";
    public List<QuestionTypeRequirement> QuestionRequirements { get; set; } = new();
    public string TeacherInstructions { get; set; } = string.Empty;
    public string IdempotencyKey { get; set; } = string.Empty;
}

public interface IExamGenerationService
{
    Task<Guid> StartGenerationAsync(GenerateExamRequest request, CancellationToken cancellationToken = default);
    Task ProcessGenerationAsync(Guid generationId, GenerateExamRequest request, CancellationToken cancellationToken = default);
}
