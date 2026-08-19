using System;
using System.Threading;
using System.Threading.Tasks;
using Draya.Domain.Exams;
using MediatR;

namespace Draya.Application.Exams.Commands.Attempts;

public record StartExamAttemptCommand(Guid ExamId, Guid StudentId) : IRequest<Guid>;

public class StartExamAttemptCommandHandler : IRequestHandler<StartExamAttemptCommand, Guid>
{
    private readonly IExamRepository _examRepository;
    private readonly IStudentExamAttemptRepository _attemptRepository;

    public StartExamAttemptCommandHandler(
        IExamRepository examRepository,
        IStudentExamAttemptRepository attemptRepository)
    {
        _examRepository = examRepository;
        _attemptRepository = attemptRepository;
    }

    public async Task<Guid> Handle(StartExamAttemptCommand request, CancellationToken cancellationToken)
    {
        var exam = await _examRepository.GetByIdAsync(request.ExamId, cancellationToken);
        if (exam == null)
        {
            throw new Exception("Exam not found");
        }

        var attempt = new StudentExamAttempt(request.ExamId, request.StudentId);
        await _attemptRepository.AddAsync(attempt, cancellationToken);

        return attempt.Id;
    }
}
