using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Draya.Domain.Exams;

namespace Draya.Application.Exams.Commands.Attempts;

public record OverrideAnswerScoreCommand(
    Guid AttemptId, 
    Guid AnswerId, 
    Guid TeacherId, 
    decimal NewScore) : IRequest<bool>;

public class OverrideAnswerScoreCommandHandler : IRequestHandler<OverrideAnswerScoreCommand, bool>
{
    private readonly IStudentExamAttemptRepository _attemptRepo;
    private readonly IMediator _mediator;

    public OverrideAnswerScoreCommandHandler(
        IStudentExamAttemptRepository attemptRepo,
        IMediator mediator)
    {
        _attemptRepo = attemptRepo;
        _mediator = mediator;
    }

    public async Task<bool> Handle(OverrideAnswerScoreCommand request, CancellationToken cancellationToken)
    {
        var attempt = await _attemptRepo.GetByIdAsync(request.AttemptId, cancellationToken);
        if (attempt == null)
        {
            return false;
        }

        var success = await _attemptRepo.OverrideAnswerScoreAsync(
            request.AttemptId, 
            request.AnswerId, 
            request.NewScore, 
            request.TeacherId, 
            cancellationToken);

        if (success)
        {
            await _mediator.Publish(new Draya.Application.Exams.Events.AnswerScoreOverriddenEvent(
                attempt.StudentId,
                request.AttemptId,
                request.AnswerId,
                request.NewScore
            ), cancellationToken);
        }

        return success;
    }
}

