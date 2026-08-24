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
        var analytics = await _analyticsService.GetAnalyticsAsync(studentId, null, cancellationToken);
        
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
                    Feature = AiFeature.ReportGeneration,
                    SystemPrompt = "You are an encouraging, expert AI tutor generating actionable recommendations for students based on their weak topics. Be supportive, concise, and provide your recommendation as a single clear paragraph without bullet points.",
                    UserPrompt = prompt,
                    RequestJsonResponse = true
                };

                string recommendation = "Review the material again.";
                try
                {
                    var response = await _llmService.GenerateAsync(request, cancellationToken);
                    var cleanContent = CleanJson(response.Content);
                    var doc = JsonDocument.Parse(cleanContent);
                    if (doc.RootElement.TryGetProperty("recommendation", out var recProp))
                    {
                        recommendation = recProp.GetString() ?? recommendation;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to generate recommendation for topic {Topic}", wt.TopicName);
                }

                weakTopics.Add(new WeakTopic(wt.TopicName, wt.SubjectName, Math.Round(wt.ProficiencyPercent, 2), wt.Status, recommendation));
            }
        }

        // Add non-urgent weak topics (Improving)
        var improvingTopics = analytics.WeakTopics.Where(x => x.Status == ProficiencyStatus.Improving).ToList();
        foreach (var wt in improvingTopics)
        {
            weakTopics.Add(new WeakTopic(wt.TopicName, wt.SubjectName, Math.Round(wt.ProficiencyPercent, 2), wt.Status, "Keep practicing this topic to reach full proficiency."));
        }

        report.AddWeakTopics(weakTopics);

        report.SetMetrics(
            analytics.TotalQuestionsAsked,
            analytics.TotalQuestionsReplied,
            Math.Round(analytics.AverageExamDurationMinutes, 2),
            analytics.CompletedLessons,
            Math.Round(analytics.ClassroomPercentile, 2)
        );

        // Generate overall summary using LLM with new metrics
        decimal percentage = analytics.OverallAverageMax > 0 ? (analytics.OverallAverage / analytics.OverallAverageMax) * 100m : 0m;
        var summaryPrompt = $@"Generate a 3-sentence performance summary for student {anonymizedId}.
Metrics:
- Overall Average: {percentage:F1}% (Classroom Percentile: Top {100m - analytics.ClassroomPercentile:F1}%)
- Engagement: Asked {analytics.TotalQuestionsAsked} questions, replied to {analytics.TotalQuestionsReplied} questions.
- Time Management: Average exam duration is {analytics.AverageExamDurationMinutes:F1} minutes.
- Material Consumption: Completed {analytics.CompletedLessons} lessons.

Instructions:
1. Provide a highly encouraging and supportive tone.
2. Praise their engagement if they ask or reply to questions; gently encourage them to participate more if engagement is 0.
3. If they finish exams very quickly (under 15 mins) and score low, kindly advise slowing down to absorb the material.
4. If they are in the top 20%, congratulate them. If their material consumption is 0, recommend studying the provided lessons.
Return ONLY a JSON object: {{ ""summary"": ""your summary text here"" }}";

        var summaryRequest = new LlmRequest
        {
            Feature = AiFeature.ReportGeneration,
            SystemPrompt = "You are an encouraging and supportive expert AI tutor summarizing student performance.",
            UserPrompt = summaryPrompt,
            RequestJsonResponse = true
        };

        string overallSummary = $"Student has completed {analytics.CompletedExams} exams with an overall average of {percentage:F1}%.";
        try
        {
            var response = await _llmService.GenerateAsync(summaryRequest, cancellationToken);
            var cleanContent = CleanJson(response.Content);
            var doc = JsonDocument.Parse(cleanContent);
            if (doc.RootElement.TryGetProperty("summary", out var sumProp))
            {
                overallSummary = sumProp.GetString() ?? overallSummary;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate overall AI summary for student {StudentId}", studentId);
        }

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

    private string CleanJson(string output)
    {
        if (string.IsNullOrWhiteSpace(output)) return "{}";
        var clean = output.Trim();
        if (clean.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
            clean = clean.Substring(7);
        else if (clean.StartsWith("```"))
            clean = clean.Substring(3);

        if (clean.EndsWith("```"))
            clean = clean.Substring(0, clean.Length - 3);

        return clean.Trim();
    }
}
