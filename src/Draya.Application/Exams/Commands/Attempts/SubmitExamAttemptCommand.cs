using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Draya.Application.Exams.Services;
using Draya.Domain.Exams;
using Draya.Domain.Exams.Exceptions;
using Draya.Domain.Identity.Exceptions;
using MediatR;

namespace Draya.Application.Exams.Commands.Attempts;

public record AnswerSubmissionDto(Guid ExamQuestionId, string? AnswerText, Guid? SelectedOptionId);

public record SubmitExamAttemptCommand(Guid AttemptId, List<AnswerSubmissionDto> Answers, string IdempotencyKey) : IRequest<Guid?>;

public class SubmitExamAttemptCommandHandler : IRequestHandler<SubmitExamAttemptCommand, Guid?>
{
    private readonly IStudentExamAttemptRepository _attemptRepository;
    private readonly IExamGradingService _examGradingService;
    private readonly IExamRepository _examRepository;

    public SubmitExamAttemptCommandHandler(
        IStudentExamAttemptRepository attemptRepository,
        IExamGradingService examGradingService,
        IExamRepository examRepository)
    {
        _attemptRepository = attemptRepository;
        _examGradingService = examGradingService;
        _examRepository = examRepository;
    }

    public async Task<Guid?> Handle(SubmitExamAttemptCommand request, CancellationToken cancellationToken)
    {
        // Custom Validation
        foreach (var a in request.Answers)
        {
            if (!a.SelectedOptionId.HasValue && string.IsNullOrWhiteSpace(a.AnswerText))
            {
                throw new ArgumentException("Either SelectedOptionId or AnswerText must be provided for all answers.");
            }
        }
        var attempt = await _attemptRepository.GetByIdAsync(request.AttemptId, cancellationToken);
        if (attempt == null)
        {
            throw new NotFoundException("Attempt not found");
        }

        if (attempt.IsSubmitted)
        {
            throw new ExamAttemptSubmissionException("Attempt has already been submitted");
        }

        var exam = await _examRepository.GetByIdAsync(attempt.ExamId, cancellationToken);
        if (exam == null)
        {
            throw new NotFoundException("Exam not found");
        }

        var now = DateTime.UtcNow;
        var maxEndTime = attempt.StartedAt.AddMinutes(exam.DurationMinutes).AddMinutes(2); // 2 min grace period
        
        if (now > maxEndTime)
        {
            throw new ExamAttemptSubmissionException("Exam duration has expired. Late submissions are not allowed.");
        }

        if (exam.EndDate.HasValue && now > exam.EndDate.Value.AddMinutes(2)) // 2 min grace period
        {
            throw new ExamAttemptSubmissionException("The exam end date has passed. Late submissions are not allowed.");
        }

        // Build the StudentAnswer list independently
        var answers = request.Answers.Select(a =>
            new StudentAnswer(attempt.Id, a.ExamQuestionId, a.AnswerText ?? string.Empty, a.SelectedOptionId)
        ).ToList();

        // Auto-grade objective questions immediately
        var questionMap = exam.Questions.ToDictionary(q => q.Id);
        decimal totalExamScore = 0;
        bool hasSubjectiveQuestions = false;
        var gradingResults = new List<AnswerGradingResult>();

        foreach (var answer in answers)
        {
            if (!questionMap.TryGetValue(answer.ExamQuestionId, out var question)) continue;

            if (question.Type is "MultipleChoice" or "TrueFalse" or "FillInTheBlank")
            {
                decimal maxScore = 1.0m;
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

                var result = new AnswerGradingResult(
                    studentAnswerId: answer.Id,
                    score: score,
                    maxScore: maxScore,
                    confidenceScore: 1.0m,
                    rationale: "Deterministic grading based on exact match.",
                    isAiGraded: false,
                    needsTeacherReview: false
                );
                
                answer.SetGradingResult(result);
                gradingResults.Add(result);
                totalExamScore += score;
            }
            else
            {
                hasSubjectiveQuestions = true;
            }
        }

        attempt.Submit();

        if (!hasSubjectiveQuestions)
        {
            var gradingResultsList = attempt.Answers
                .Where(a => a.GradingResult != null)
                .Select(a => a.GradingResult!)
                .ToList();
            
            // Hardcode 1.0m per question if not specified
            decimal totalMaxScore = attempt.Answers.Count * 1.0m;
            attempt.UpdateFinalScore(totalExamScore, totalMaxScore, false);

            await _attemptRepository.SaveGradingResultsAsync(attempt, gradingResultsList, cancellationToken);
            return null; // No background job needed
        }

        // If there are subjective questions, save without final score and start background job
        await _attemptRepository.SubmitAsync(attempt, answers, cancellationToken);

        // Trigger grading pipeline for the remaining subjective questions
        var gradingRequest = new StartGradingRequest
        {
            StudentExamAttemptId = attempt.Id,
            IdempotencyKey = string.IsNullOrWhiteSpace(request.IdempotencyKey) ? Guid.NewGuid().ToString() : request.IdempotencyKey
        };

        var jobId = await _examGradingService.StartGradingAsync(gradingRequest, cancellationToken);
        return jobId;
    }
}
