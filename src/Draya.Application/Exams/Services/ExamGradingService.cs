using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Draya.Application.AI;
using Draya.Application.AI.Models;
using Draya.Application.Common.Interfaces;
using Draya.Application.Exams.Events;
using Draya.Domain.Exams;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Draya.Application.Exams.Services;

public class ExamGradingService : IExamGradingService
{
    private readonly IExamGradingJobRepository _jobRepo;
    private readonly IStudentExamAttemptRepository _attemptRepo;
    private readonly IExamRepository _examRepo;
    private readonly IExamGradingTaskQueue _taskQueue;
    private readonly ILLMService _llmService;
    private readonly IPiiAnonymizer _piiAnonymizer;
    private readonly ILogger<ExamGradingService> _logger;
    private readonly IPublisher _publisher;

    private const decimal ConfidenceThreshold = 0.85m;

    public ExamGradingService(
        IExamGradingJobRepository jobRepo,
        IStudentExamAttemptRepository attemptRepo,
        IExamRepository examRepo,
        IExamGradingTaskQueue taskQueue,
        ILLMService llmService,
        IPiiAnonymizer piiAnonymizer,
        ILogger<ExamGradingService> logger,
        IPublisher publisher)
    {
        _jobRepo = jobRepo;
        _attemptRepo = attemptRepo;
        _examRepo = examRepo;
        _taskQueue = taskQueue;
        _llmService = llmService;
        _piiAnonymizer = piiAnonymizer;
        _logger = logger;
        _publisher = publisher;
    }

    public async Task<Guid> StartGradingAsync(StartGradingRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await _jobRepo.GetByIdempotencyKeyAsync(request.IdempotencyKey, cancellationToken);
        if (existing != null) return existing.Id;

        var job = new ExamGradingJob(request.StudentExamAttemptId, request.IdempotencyKey);
        await _jobRepo.AddAsync(job, cancellationToken);

        await _taskQueue.QueueBackgroundWorkItemAsync(new ExamGradingItem(job.Id, request.StudentExamAttemptId));

        return job.Id;
    }

    public async Task ProcessGradingAsync(Guid gradingJobId, Guid studentExamAttemptId, CancellationToken cancellationToken = default)
    {
        var job = await _jobRepo.GetByIdAsync(gradingJobId, cancellationToken);
        if (job == null) return;

        try
        {
            job.UpdateStatus(GradingStatus.Grading);
            await _jobRepo.UpdateAsync(job, cancellationToken);
            
            var attempt = await _attemptRepo.GetByIdAsync(studentExamAttemptId, cancellationToken);
            if (attempt == null) throw new Exception("Exam attempt not found");
            
            await _publisher.Publish(new ExamGradingProgressEvent(
                job.Id, attempt.StudentId, studentExamAttemptId, GradingStatus.Grading, null, null, false), cancellationToken);

            var exam = await _examRepo.GetByIdAsync(attempt.ExamId, cancellationToken);
            if (exam == null) throw new Exception("Exam not found");

            var questionMap = exam.Questions.ToDictionary(q => q.Id);
            bool hasWarnings = false;
            decimal totalExamScore = 0;
            bool examNeedsReview = false;

            var anonymizedStudentId = await _piiAnonymizer.GetAnonymizedIdAsync(attempt.StudentId, cancellationToken);

            foreach (var answer in attempt.Answers)
            {
                if (!questionMap.TryGetValue(answer.ExamQuestionId, out var question)) continue;

                // Simple assumption: each question is worth 1 point for now, 
                // but usually the max score is stored in the domain. 
                // We'll hardcode MaxScore = 1.0m unless specified.
                decimal maxScore = 1.0m;
                
                AnswerGradingResult result;

                if (IsObjectiveQuestion(question.Type))
                {
                    result = GradeObjectiveQuestion(answer, question, maxScore);
                }
                else
                {
                    result = await GradeSubjectiveQuestionAsync(anonymizedStudentId, answer, question, maxScore, cancellationToken);
                }

                answer.SetGradingResult(result);
                totalExamScore += result.Score;
                
                if (result.NeedsTeacherReview)
                {
                    hasWarnings = true;
                    examNeedsReview = true;
                }
            }

            // Persist grading results: add AnswerGradingResult entities directly to DbContext
            // to avoid EF Core backing-field collection tracking issues (same pattern as SubmitAsync).
            var gradingResults = attempt.Answers
                .Where(a => a.GradingResult != null)
                .Select(a => a.GradingResult!)
                .ToList();
            
            attempt.UpdateFinalScore(totalExamScore, examNeedsReview);
            await _attemptRepo.SaveGradingResultsAsync(attempt, gradingResults, cancellationToken);

            var finalStatus = hasWarnings ? GradingStatus.CompletedWithWarning : GradingStatus.Completed;
            job.UpdateStatus(finalStatus);
            await _jobRepo.UpdateAsync(job, cancellationToken);
            
            await _publisher.Publish(new ExamGradingProgressEvent(
                job.Id, attempt.StudentId, studentExamAttemptId, finalStatus, null, totalExamScore, examNeedsReview), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Grading job {JobId} failed", gradingJobId);
            job.UpdateStatus(GradingStatus.Failed, ex.Message);
            await _jobRepo.UpdateAsync(job, cancellationToken);
            
            // We might not have attempt info if it failed early, so StudentId could be empty
            var attempt = await _attemptRepo.GetByIdAsync(studentExamAttemptId, cancellationToken);
            await _publisher.Publish(new ExamGradingProgressEvent(
                job.Id, attempt?.StudentId ?? Guid.Empty, studentExamAttemptId, GradingStatus.Failed, ex.Message, null, false), cancellationToken);
        }
    }

    private bool IsObjectiveQuestion(string type)
    {
        return type is "MultipleChoice" or "TrueFalse" or "FillInTheBlank";
    }

    private AnswerGradingResult GradeObjectiveQuestion(StudentAnswer answer, ExamQuestion question, decimal maxScore)
    {
        decimal score = 0;
        
        if (question.Type is "MultipleChoice" or "TrueFalse")
        {
            var correctOption = question.Options.FirstOrDefault(o => o.IsCorrect);
            if (correctOption != null && answer.SelectedOptionId == correctOption.Id)
            {
                score = maxScore;
            }
        }
        else if (question.Type == "FillInTheBlank")
        {
            var isCorrect = question.Options.Any(o => o.IsCorrect && o.Text.Equals(answer.AnswerText?.Trim(), StringComparison.OrdinalIgnoreCase));
            if (isCorrect) score = maxScore;
        }

        return new AnswerGradingResult(
            studentAnswerId: answer.Id,
            score: score,
            maxScore: maxScore,
            confidenceScore: 1.0m, // Deterministic has 100% confidence
            rationale: "Deterministic grading based on exact match.",
            isAiGraded: false,
            needsTeacherReview: false
        );
    }

    private async Task<AnswerGradingResult> GradeSubjectiveQuestionAsync(Guid anonymizedStudentId, StudentAnswer answer, ExamQuestion question, decimal maxScore, CancellationToken cancellationToken)
    {
        var systemPrompt = $@"You are an expert AI Grader. Your task is to evaluate a student's answer to a subjective question.
You MUST output a valid JSON object.
Do NOT reveal the student's identity (anonymized ID: {anonymizedStudentId}).

# Question Details
Type: {question.Type}
Text: {question.Text}
Rubric/Ideal Answer: {question.Rubric ?? "N/A"}
Max Score: {maxScore}

# Instructions
Evaluate the answer based strictly on the rubric.
Calculate a 'score' between 0 and {maxScore}.
Provide a 'confidenceScore' between 0.0 and 1.0 indicating your certainty.
Provide a concise 'rationale'.

Output JSON schema:
{{
  ""score"": 0.5,
  ""confidenceScore"": 0.9,
  ""rationale"": ""Explanation of the grade""
}}";

        var userPrompt = $"Student Answer:\n{answer.AnswerText}";

        var llmRequest = new LlmRequest
        {
            Feature = AiFeature.ExamGrading,
            SystemPrompt = systemPrompt,
            UserPrompt = userPrompt,
            RequestJsonResponse = true
        };

        decimal score = 0;
        decimal confidence = 0;
        string rationale = "AI Grading failed";
        bool needsReview = true;

        try
        {
            var llmResponse = await _llmService.GenerateAsync(llmRequest, cancellationToken);
            var jsonContent = CleanLlmJsonResponse(llmResponse.Content);
            
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var aiResult = JsonSerializer.Deserialize<AiGradingResponse>(jsonContent, options);

            if (aiResult != null)
            {
                // Enforce max score boundary
                score = Math.Min(Math.Max(aiResult.Score, 0), maxScore);
                confidence = Math.Max(aiResult.ConfidenceScore, 0);
                rationale = aiResult.Rationale;
                
                if (confidence >= ConfidenceThreshold)
                {
                    needsReview = false;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI grading failed for Answer {AnswerId}", answer.Id);
        }

        return new AnswerGradingResult(
            studentAnswerId: answer.Id,
            score: score,
            maxScore: maxScore,
            confidenceScore: confidence,
            rationale: rationale,
            isAiGraded: true,
            needsTeacherReview: needsReview
        );
    }

    private string CleanLlmJsonResponse(string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return content;
        
        var startIndex = content.IndexOf('{');
        var endIndex = content.LastIndexOf('}');
        
        if (startIndex >= 0 && endIndex > startIndex)
        {
            return content.Substring(startIndex, endIndex - startIndex + 1);
        }
        
        return content.Trim();
    }
    
    private class AiGradingResponse
    {
        public decimal Score { get; set; }
        public decimal ConfidenceScore { get; set; }
        public string Rationale { get; set; } = string.Empty;
    }
}
