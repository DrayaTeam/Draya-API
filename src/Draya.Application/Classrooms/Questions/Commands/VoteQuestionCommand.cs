using Draya.Application.Classrooms.Questions.Notifications;
using Draya.Domain.Classrooms;
using MediatR;

namespace Draya.Application.Classrooms.Questions.Commands;

public record VoteQuestionCommand(
    Guid QuestionId,
    Guid CurrentUserId
) : IRequest;

public class VoteQuestionCommandHandler : IRequestHandler<VoteQuestionCommand>
{
    private readonly IQuestionRepository _questionRepository;
    private readonly IClassroomRepository _classroomRepository;
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly IMediator _mediator;

    public VoteQuestionCommandHandler(
        IQuestionRepository questionRepository,
        IClassroomRepository classroomRepository,
        IEnrollmentRepository enrollmentRepository,
        IMediator mediator)
    {
        _questionRepository = questionRepository;
        _classroomRepository = classroomRepository;
        _enrollmentRepository = enrollmentRepository;
        _mediator = mediator;
    }

    public async Task Handle(VoteQuestionCommand request, CancellationToken cancellationToken)
    {
        var question = await _questionRepository.GetByIdAsync(request.QuestionId, cancellationToken);
        if (question == null)
            throw new KeyNotFoundException("Question not found.");

        var classroom = await _classroomRepository.GetByIdAsync(question.ClassroomId, cancellationToken);
        if (classroom == null)
            throw new UnauthorizedAccessException();

        if (classroom.TeacherId != request.CurrentUserId)
        {
            var enrollment = await _enrollmentRepository.GetByStudentAndClassroomAsync(request.CurrentUserId, question.ClassroomId, cancellationToken);
            if (enrollment == null || enrollment.Status != EnrollmentStatus.Active)
                throw new UnauthorizedAccessException("Not enrolled in this classroom.");
        }

        var hasVoted = await _questionRepository.HasUserVotedAsync(request.QuestionId, request.CurrentUserId, cancellationToken);
        if (hasVoted)
        {
            return; // Already voted, ignore or throw based on preference, returning is idempotent.
        }

        var vote = new QuestionVote
        {
            QuestionId = request.QuestionId,
            UserId = request.CurrentUserId
        };

        try
        {
            await _questionRepository.AddVoteAsync(vote, cancellationToken);
            
            question.VoteCount++;
            await _questionRepository.UpdateAsync(question, cancellationToken);

            await _mediator.Publish(new QuestionVoteUpdatedNotification(
                question.ClassroomId,
                question.Id,
                question.VoteCount
            ), cancellationToken);
        }
        catch (Exception ex)
        {
            // Handle unique constraint violation gracefully if concurrent votes happen
            // (e.g. DbUpdateException / SqlException with violation of primary key constraint)
            if (ex.InnerException?.Message.Contains("Violation of PRIMARY KEY constraint") == true)
            {
                return;
            }
            throw;
        }
    }
}
