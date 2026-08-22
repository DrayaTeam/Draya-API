using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Draya.Application.AI;
using Draya.Application.AI.Models;
using Draya.Application.Exams.Services;
using Draya.Application.Materials.RAG;
using Draya.Application.Reports.Services;
using Draya.Domain.Reports;
using Draya.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Draya.Infrastructure.Reports.Services;

public class InteractiveReviewService : IInteractiveReviewService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IRetrievalService _retrievalService;
    private readonly IExamGenerationService _examGenerationService;
    private readonly ILLMService _llmService;

    public InteractiveReviewService(
        ApplicationDbContext dbContext,
        IRetrievalService retrievalService,
        IExamGenerationService examGenerationService,
        ILLMService llmService)
    {
        _dbContext = dbContext;
        _retrievalService = retrievalService;
        _examGenerationService = examGenerationService;
        _llmService = llmService;
    }

    public async Task<TopicRevisionDto> GetRevisionAsync(Guid studentId, string topicName, CancellationToken cancellationToken = default)
    {
        // 1. Get recommendation from latest PerformanceReport
        var latestReport = await _dbContext.PerformanceReports
            .Where(r => r.StudentId == studentId)
            .OrderByDescending(r => r.GeneratedAt)
            .FirstOrDefaultAsync(cancellationToken);

        string recommendation = "Review the material related to this topic.";
        if (latestReport != null)
        {
            var weakTopic = latestReport.WeakTopics.FirstOrDefault(w => w.TopicName.Equals(topicName, StringComparison.OrdinalIgnoreCase));
            if (weakTopic != null)
            {
                recommendation = weakTopic.Recommendation;
            }
        }

        // 2. Fetch Source Materials using RetrievalService
        // Find material version IDs for classrooms student is in
        var enrolledClassrooms = _dbContext.Enrollments
            .Where(e => e.StudentId == studentId && e.Status == Draya.Domain.Classrooms.EnrollmentStatus.Active)
            .Select(e => e.ClassroomId);

        var materialVersions = await _dbContext.LearningMaterials
            .Where(m => enrolledClassrooms.Contains(m.ClassroomId))
            .SelectMany(m => m.Versions)
            .Select(v => v.Id)
            .ToListAsync(cancellationToken);

        var sourceMaterials = new List<string>();

        if (materialVersions.Any())
        {
            var query = new RetrievalQuery
            {
                QueryText = topicName,
                MaterialVersionIds = materialVersions,
                TopK = 3,
                MinScore = 0.6f
            };

            var results = await _retrievalService.SearchAsync(query, cancellationToken);
            sourceMaterials = results.Select(r => r.Text).ToList();
        }

        string aiExplanation = "We couldn't generate a specific explanation because no reference materials were found in your classroom for this topic. Please check your classroom materials.";

        if (sourceMaterials.Any())
        {
            var prompt = $@"
You are a helpful and expert AI tutor. 
A student is struggling with the topic: '{topicName}'. 
Using ONLY the following course material excerpts, explain this topic clearly and comprehensively to the student. 
Go 'under the hood' to explain the concepts so they can understand it well.
Format your response in clean Markdown.

Course Materials:
{string.Join("\n\n---\n\n", sourceMaterials)}
";

            var llmRequest = new LlmRequest
            {
                SystemPrompt = "You are an expert tutor.",
                UserPrompt = prompt,
                RequestJsonResponse = false
            };

            try
            {
                var llmResponse = await _llmService.GenerateAsync(llmRequest, cancellationToken);
                aiExplanation = llmResponse?.Content ?? aiExplanation;
            }
            catch (Exception)
            {
                // LLM call failed — return fallback message instead of crashing with 500
                aiExplanation = $"We were unable to generate an AI explanation for '{topicName}' at this time due to a temporary service issue. " +
                                "Please review your classroom materials directly and try again later.";
            }
        }

        return new TopicRevisionDto(recommendation, aiExplanation);
    }

    public async Task<Guid> GeneratePracticeExamAsync(Guid studentId, string topicName, PracticeExamRequest request, CancellationToken cancellationToken = default)
    {
        // For practice exam, find classrooms
        var enrolledClassrooms = await _dbContext.Enrollments
            .Where(e => e.StudentId == studentId && e.Status == Draya.Domain.Classrooms.EnrollmentStatus.Active)
            .Select(e => e.ClassroomId)
            .ToListAsync(cancellationToken);

        if (!enrolledClassrooms.Any())
            throw new Exception("Student is not enrolled in any classrooms.");

        var classroomId = enrolledClassrooms.First();

        // Get a valid section for the classroom to avoid FK violations on Exam creation
        var sectionId = await _dbContext.ClassroomSections
            .Where(s => s.ClassroomId == classroomId)
            .Select(s => s.Id)
            .FirstOrDefaultAsync(cancellationToken);

        // We can just use the Topic field. For "ExamType = PracticeReview" to bypass wallet, 
        // we actually need to add a flag or bypass it in the handler. The prompt said "with a PracticeReview flag".
        // Let's add IsPracticeReview to GenerateExamRequest.

        var genRequest = new Draya.Application.Exams.Services.GenerateExamRequest
        {
            TeacherId = Guid.Empty, // Or fetch the teacher ID
            ClassroomId = classroomId,
            SectionId = sectionId, // Pass valid section instead of Guid.Empty
            Topic = topicName,
            DifficultyLevel = "Medium",
            DurationMinutes = 15,
            StartDate = DateTime.UtcNow.AddMinutes(1),
            EndDate = DateTime.UtcNow.AddDays(1),
            AllowedAttempts = 1,
            QuestionRequirements = new List<Draya.Application.Exams.Services.QuestionTypeRequirement> 
            { 
                new Draya.Application.Exams.Services.QuestionTypeRequirement { Type = "MultipleChoice", Count = 3 },
                new Draya.Application.Exams.Services.QuestionTypeRequirement { Type = "TrueFalse", Count = 2 }
            },
            TeacherInstructions = $"Focus solely on the topic: {topicName}",
            IsPracticeReview = true
        };

        var idempotencyKey = $"practice_{studentId}_{topicName}_{DateTime.UtcNow.Ticks}";
        genRequest.IdempotencyKey = idempotencyKey;

        var jobId = await _examGenerationService.StartGenerationAsync(genRequest, cancellationToken);
        return jobId;
    }
}
