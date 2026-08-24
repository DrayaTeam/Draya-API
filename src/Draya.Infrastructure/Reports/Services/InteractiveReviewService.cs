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
        var topicId = Draya.Application.Utils.GuidUtility.Create(Draya.Application.Utils.GuidUtility.IsoOidNamespace, topicName ?? "General");

        var weakness = await _dbContext.StudentWeaknesses
            .FirstOrDefaultAsync(w => w.StudentId == studentId && w.TopicId == topicId, cancellationToken);

        if (weakness == null)
        {
            return new TopicRevisionDto("No weakness found for this topic.", "You are currently not marked as weak in this topic.");
        }

        var cachedReview = await _dbContext.WeaknessReviews
            .FirstOrDefaultAsync(r => r.StudentWeaknessId == weakness.Id && !r.IsOutdated, cancellationToken);

        if (cachedReview != null)
        {
            return new TopicRevisionDto(cachedReview.Recommendations, cachedReview.AiExplanation);
        }

        // 2. Fetch Source Materials using RetrievalService
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
        string recommendation = "Review the material related to this topic.";

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
                aiExplanation = $"We were unable to generate an AI explanation for '{topicName}' at this time due to a temporary service issue. " +
                                "Please review your classroom materials directly and try again later.";
            }
        }

        // Save new active review
        var newReview = new WeaknessReview(
            weakness.Id,
            weakness.CurrentProficiencyPercent,
            aiExplanation,
            "Key concepts derived from AI", // Could be parsed if we asked JSON
            "Common mistakes derived from AI",
            recommendation
        );

        await _dbContext.WeaknessReviews.AddAsync(newReview, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new TopicRevisionDto(recommendation, aiExplanation);
    }

    public async Task<Guid> GeneratePracticeExamAsync(Guid studentId, string topicName, PracticeExamRequest request, CancellationToken cancellationToken = default)
    {
        // Find the student's active enrolled classrooms
        var enrolledClassrooms = await _dbContext.Enrollments
            .Where(e => e.StudentId == studentId && e.Status == Draya.Domain.Classrooms.EnrollmentStatus.Active)
            .Select(e => e.ClassroomId)
            .ToListAsync(cancellationToken);

        if (!enrolledClassrooms.Any())
            throw new Draya.Domain.Classrooms.Exceptions.StudentNotEnrolledException();

        var classroomId = enrolledClassrooms.First();
        var sectionId = Guid.Empty;

        // Trace the weakness back to the exact section it came from:
        //   StudentWeakness → StudentWeaknessHistory.SourceAttemptId
        //   → StudentExamAttempt.ExamId → Exam.SectionId
        // This scopes the RAG retrieval to only the relevant course material section,
        // reducing token usage, latency, and off-topic noise.
        var topicId = Draya.Application.Utils.GuidUtility.Create(
            Draya.Application.Utils.GuidUtility.IsoOidNamespace, topicName ?? "General");

        var weakness = await _dbContext.StudentWeaknesses
            .FirstOrDefaultAsync(w => w.StudentId == studentId && w.TopicId == topicId, cancellationToken);

        if (weakness != null)
        {
            // Get the most recent history entry that has a source attempt
            var latestHistory = await _dbContext.StudentWeaknessHistories
                .Where(h => h.StudentWeaknessId == weakness.Id && h.SourceAttemptId != null)
                .OrderByDescending(h => h.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (latestHistory?.SourceAttemptId != null)
            {
                var examId = await _dbContext.StudentExamAttempts
                    .Where(a => a.Id == latestHistory.SourceAttemptId)
                    .Select(a => a.ExamId)
                    .FirstOrDefaultAsync(cancellationToken);

                if (examId != Guid.Empty)
                {
                    sectionId = await _dbContext.Exams
                        .Where(e => e.Id == examId)
                        .Select(e => e.SectionId)
                        .FirstOrDefaultAsync(cancellationToken);
                }
            }
        }

        var genRequest = new Draya.Application.Exams.Services.GenerateExamRequest
        {
            TeacherId = Guid.Empty,
            ClassroomId = classroomId,
            // If we traced a section, use it — scopes retrieval tightly to the right material.
            // If not found, fall back to Guid.Empty which triggers classroom-wide retrieval.
            SectionId = sectionId,
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
            IsPracticeReview = true,
            IdempotencyKey = $"practice_{studentId}_{topicName}_{DateTime.UtcNow.Ticks}"
        };

        var jobId = await _examGenerationService.StartGenerationAsync(genRequest, cancellationToken);
        return jobId;
    }
}
