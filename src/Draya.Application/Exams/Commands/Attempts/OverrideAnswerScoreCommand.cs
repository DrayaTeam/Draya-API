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

    public OverrideAnswerScoreCommandHandler(IStudentExamAttemptRepository attemptRepo)
    {
        _attemptRepo = attemptRepo;
    }

    public async Task<bool> Handle(OverrideAnswerScoreCommand request, CancellationToken cancellationToken)
    {
        return await _attemptRepo.OverrideAnswerScoreAsync(
            request.AttemptId, 
            request.AnswerId, 
            request.NewScore, 
            request.TeacherId, 
            cancellationToken);
    }
}

