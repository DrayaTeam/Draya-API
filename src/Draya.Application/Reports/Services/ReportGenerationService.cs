using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Draya.Application.AI;
using Draya.Application.AI.Models;
using Draya.Application.Common.Interfaces;
using Draya.Application.Reports.Events;
using Draya.Domain.Reports;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Draya.Application.Reports.Services;

public class ReportGenerationService : IReportGenerationService
{
    private readonly IStudentAnalyticsService _analyticsService;
    private readonly ILLMService _llmService;
    private readonly IPiiAnonymizer _piiAnonymizer;
    private readonly IPerformanceReportRepository _reportRepository;
    private readonly IMediator _mediator;
    private readonly ILogger<ReportGenerationService> _logger;

    public ReportGenerationService(
        IStudentAnalyticsService analyticsService,
        ILLMService llmService,
        IPiiAnonymizer piiAnonymizer,
        IPerformanceReportRepository reportRepository,
        IMediator mediator,
        ILogger<ReportGenerationService> logger)
    {
        _analyticsService = analyticsService;
        _llmService = llmService;
        _piiAnonymizer = piiAnonymizer;
        _reportRepository = reportRepository;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task GenerateReportAsync(Guid studentId, Guid examAttemptId, CancellationToken cancellationToken = default)
    {
        // 1. Get analytics
        var analytics = await _analyticsService.GetAnalyticsAsync(studentId, cancellationToken);
        
        // 2. Anonymize student ID for AI (if needed)
        string anonymizedId = (await _piiAnonymizer.GetAnonymizedIdAsync(studentId, cancellationToken)).ToString();

        var report = new PerformanceReport(studentId, examAttemptId);
        
        report.AddSubjectProficiencies(analytics.SubjectProficiencies.Select(sp => new SubjectProficiency(sp.SubjectName, sp.ProficiencyPercent)));
        report.AddTrendPoints(analytics.TrendPoints.Select(tp => new TrendPoint(tp.Month, tp.AverageScore)));

        var weakTopics = new System.Collections.Generic.List<WeakTopic>();

        // 3. Process Weak Topics with LLM
        var urgentTopics = analytics.WeakTopics.Where(x => x.Status == ProficiencyStatus.NeedsUrgentImprovement).ToList();

        if (urgentTopics.Any())
        {
            foreach (var wt in urgentTopics)
            {
                // Call LLM for each urgent topic
                var prompt = $@"Student {anonymizedId} has a weak proficiency of {wt.ProficiencyPercent:F1}% in the topic '{wt.TopicName}' (Subject: {wt.SubjectName}).
Here are some examples of their incorrect answers:
{string.Join("\n", wt.ExampleIncorrectAnswers)}

Based on these incorrect answers, provide a short actionable recommendation for the student to improve. Return ONLY a JSON object: {{ ""recommendation"": ""your recommendation text here"" }}";

                var request = new LlmRequest
                {
                    SystemPrompt = "You are an expert AI tutor generating actionable recommendations for students based on their weak topics.",
                    UserPrompt = prompt,
                    RequestJsonResponse = true
                };

                string recommendation = "Review the material again.";
                try
                {
                    var response = await _llmService.GenerateAsync(request, cancellationToken);
                    var doc = JsonDocument.Parse(response.Content);
                    if (doc.RootElement.TryGetProperty("recommendation", out var recProp))
                    {
                        recommendation = recProp.GetString() ?? recommendation;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to generate recommendation for topic {Topic}", wt.TopicName);
                }

                weakTopics.Add(new WeakTopic(wt.TopicName, wt.SubjectName, wt.ProficiencyPercent, wt.Status, recommendation));
            }
        }

        // Add non-urgent weak topics (Improving)
        var improvingTopics = analytics.WeakTopics.Where(x => x.Status == ProficiencyStatus.Improving).ToList();
        foreach (var wt in improvingTopics)
        {
            weakTopics.Add(new WeakTopic(wt.TopicName, wt.SubjectName, wt.ProficiencyPercent, wt.Status, "Keep practicing this topic to reach full proficiency."));
        }

        report.AddWeakTopics(weakTopics);

        // Generate overall summary
        string overallSummary = $"Student has completed {analytics.CompletedExams} exams with an overall average of {analytics.OverallAverage:F1}%.";
        report.SetAiSummary(overallSummary);

        // Save report
        await _reportRepository.AddAsync(report, cancellationToken);

        // Trigger events
        await _mediator.Publish(new ReportGeneratedEvent(report.Id, studentId), cancellationToken);

        if (urgentTopics.Any())
        {
            var topTopic = urgentTopics.First();
            await _mediator.Publish(new StudentAtRiskEvent(studentId, topTopic.TopicName), cancellationToken);
        }
    }
}
