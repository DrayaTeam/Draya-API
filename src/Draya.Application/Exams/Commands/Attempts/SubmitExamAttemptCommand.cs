using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Draya.Application.Exams.Services;
using Draya.Domain.Exams;
using MediatR;

namespace Draya.Application.Exams.Commands.Attempts;

public record AnswerSubmissionDto(Guid ExamQuestionId, string AnswerText, Guid? SelectedOptionId);

public record SubmitExamAttemptCommand(Guid AttemptId, List<AnswerSubmissionDto> Answers, string IdempotencyKey) : IRequest<Guid>;

public class SubmitExamAttemptCommandHandler : IRequestHandler<SubmitExamAttemptCommand, Guid>
{
    private readonly IStudentExamAttemptRepository _attemptRepository;
    private readonly IExamGradingService _examGradingService;

    public SubmitExamAttemptCommandHandler(
        IStudentExamAttemptRepository attemptRepository,
        IExamGradingService examGradingService)
    {
        _attemptRepository = attemptRepository;
        _examGradingService = examGradingService;
    }

    public async Task<Guid> Handle(SubmitExamAttemptCommand request, CancellationToken cancellationToken)
    {
        var attempt = await _attemptRepository.GetByIdAsync(request.AttemptId, cancellationToken);
        if (attempt == null)
        {
            throw new Exception("Attempt not found");
        }

        if (attempt.IsSubmitted)
        {
            throw new Exception("Attempt has already been submitted");
        }

        // Build the StudentAnswer list independently — do NOT add to the aggregate.
        // Adding via attempt.AddAnswer() causes EF Core relationship tracking confusion
        // because the backing field (_answers) bypasses standard change detection.
        var answers = request.Answers.Select(a =>
            new StudentAnswer(attempt.Id, a.ExamQuestionId, a.AnswerText, a.SelectedOptionId)
        ).ToList();

        // Call Submit() to set IsSubmitted=true and SubmittedAt on the tracked entity.
        attempt.Submit();

        // Persist: explicitly mark modified properties + INSERT answers directly via DbSet.
        await _attemptRepository.SubmitAsync(attempt, answers, cancellationToken);

        // Trigger grading pipeline
        var gradingRequest = new StartGradingRequest
        {
            StudentExamAttemptId = attempt.Id,
            IdempotencyKey = string.IsNullOrWhiteSpace(request.IdempotencyKey) ? Guid.NewGuid().ToString() : request.IdempotencyKey
        };

        var jobId = await _examGradingService.StartGradingAsync(gradingRequest, cancellationToken);
        return jobId;
    }
}
