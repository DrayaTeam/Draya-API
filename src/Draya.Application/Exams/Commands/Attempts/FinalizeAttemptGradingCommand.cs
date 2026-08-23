using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Draya.Domain.Exams;

namespace Draya.Application.Exams.Commands.Attempts;

public record FinalizeAttemptGradingCommand(Guid AttemptId) : IRequest<bool>;

public class FinalizeAttemptGradingCommandHandler : IRequestHandler<FinalizeAttemptGradingCommand, bool>
{
    private readonly IStudentExamAttemptRepository _attemptRepo;

    public FinalizeAttemptGradingCommandHandler(IStudentExamAttemptRepository attemptRepo)
    {
        _attemptRepo = attemptRepo;
    }

    public async Task<bool> Handle(FinalizeAttemptGradingCommand request, CancellationToken cancellationToken)
    {
        return await _attemptRepo.FinalizeAttemptAndWeaknessesAsync(request.AttemptId, cancellationToken);
    }
}
