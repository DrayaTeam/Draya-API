using System;
using System.Threading;
using System.Threading.Tasks;
using Draya.Domain.Classrooms;
using Draya.Domain.Exams;
using MediatR;

namespace Draya.Application.Exams.Commands.Attempts;

public record StartExamAttemptCommand(Guid ExamId, Guid StudentId) : IRequest<Guid>;

public class StartExamAttemptCommandHandler : IRequestHandler<StartExamAttemptCommand, Guid>
{
    private readonly IExamRepository _examRepository;
    private readonly IStudentExamAttemptRepository _attemptRepository;
    private readonly IClassroomRepository _classroomRepository;

    public StartExamAttemptCommandHandler(
        IExamRepository examRepository,
        IStudentExamAttemptRepository attemptRepository,
        IClassroomRepository classroomRepository)
    {
        _examRepository = examRepository;
        _attemptRepository = attemptRepository;
        _classroomRepository = classroomRepository;
    }

    public async Task<Guid> Handle(StartExamAttemptCommand request, CancellationToken cancellationToken)
    {
        var exam = await _examRepository.GetByIdAsync(request.ExamId, cancellationToken);
        if (exam == null)
        {
            throw new Draya.Domain.Identity.Exceptions.NotFoundException("Exam not found");
        }

        var now = DateTime.UtcNow;
        
        if (now < exam.StartDate)
        {
            throw new Draya.Domain.Exams.Exceptions.ExamAttemptSubmissionException("The exam has not started yet.");
        }

        if (exam.EndDate.HasValue && now > exam.EndDate.Value)
        {
            throw new Draya.Domain.Exams.Exceptions.ExamAttemptSubmissionException("The exam has already ended.");
        }

        var isEnrolled = await _classroomRepository.IsStudentEnrolledAsync(request.StudentId, exam.ClassroomId, cancellationToken);
        if (!isEnrolled)
        {
            throw new UnauthorizedAccessException("Student is not enrolled in the classroom for this exam.");
        }

        var activeAttempt = await _attemptRepository.GetActiveAttemptAsync(request.StudentId, request.ExamId, cancellationToken);
        if (activeAttempt != null)
        {
            return activeAttempt.Id; // Idempotency
        }

        var existingAttempts = await _attemptRepository.GetCountByStudentAndExamAsync(request.StudentId, request.ExamId, cancellationToken);
        if (existingAttempts >= exam.AllowedAttempts)
        {
            throw new Draya.Domain.Exams.Exceptions.ExamAttemptSubmissionException($"You have reached the maximum allowed attempts ({exam.AllowedAttempts}) for this exam.");
        }

        var attempt = new StudentExamAttempt(request.ExamId, request.StudentId);
        await _attemptRepository.AddAsync(attempt, cancellationToken);

        return attempt.Id;
    }
}
