using Draya.Application.Classrooms.Questions.Notifications;
using Draya.Domain.Classrooms;
using MediatR;

namespace Draya.Application.Classrooms.Questions.Commands;

public record RemoveVoteCommand(
    Guid QuestionId,
    Guid CurrentUserId
) : IRequest;

public class RemoveVoteCommandHandler : IRequestHandler<RemoveVoteCommand>
{
    private readonly IQuestionRepository _questionRepository;
    private readonly IClassroomRepository _classroomRepository;
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly IMediator _mediator;

    public RemoveVoteCommandHandler(
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

    public async Task Handle(RemoveVoteCommand request, CancellationToken cancellationToken)
    {
        var question = await _questionRepository.GetByIdAsync(request.QuestionId, cancellationToken);
        if (question == null)
            throw new KeyNotFoundException("Question not found.");

        var hasVoted = await _questionRepository.HasUserVotedAsync(request.QuestionId, request.CurrentUserId, cancellationToken);
        if (!hasVoted)
        {
            return; // Not voted
        }

        await _questionRepository.RemoveVoteAsync(request.QuestionId, request.CurrentUserId, cancellationToken);
        
        question.VoteCount = Math.Max(0, question.VoteCount - 1);
        await _questionRepository.UpdateAsync(question, cancellationToken);

        await _mediator.Publish(new QuestionVoteUpdatedNotification(
            question.ClassroomId,
            question.Id,
            question.VoteCount
        ), cancellationToken);
    }
}
