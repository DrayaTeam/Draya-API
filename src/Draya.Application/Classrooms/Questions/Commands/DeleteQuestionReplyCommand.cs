using Draya.Domain.Identity.Exceptions;
using Draya.Domain.Classrooms;
using MediatR;

namespace Draya.Application.Classrooms.Questions.Commands;

public record DeleteQuestionReplyCommand(Guid ReplyId, Guid UserId) : IRequest<Unit>;

public class DeleteQuestionReplyCommandHandler : IRequestHandler<DeleteQuestionReplyCommand, Unit>
{
    private readonly IQuestionRepository _questionRepository;

    public DeleteQuestionReplyCommandHandler(IQuestionRepository questionRepository)
    {
        _questionRepository = questionRepository;
    }

    public async Task<Unit> Handle(DeleteQuestionReplyCommand request, CancellationToken cancellationToken)
    {
        var reply = await _questionRepository.GetReplyByIdAsync(request.ReplyId, cancellationToken);
        if (reply == null)
            throw new NotFoundException($"Reply {request.ReplyId} not found.");

        if (reply.AuthorId != request.UserId)
            throw new UnauthorizedAccessException("You can only delete your own replies.");

        await _questionRepository.DeleteReplyAsync(reply, cancellationToken);

        // Update question metrics
        var question = await _questionRepository.GetByIdAsync(reply.QuestionId, cancellationToken);
        if (question != null)
        {
            if (question.ReplyCount > 0)
            {
                question.ReplyCount--;
            }

            if (reply.IsTeacherAnswer)
            {
                question.HasTeacherAnswer = await _questionRepository.HasTeacherReplyAsync(question.Id, cancellationToken);
            }

            await _questionRepository.UpdateAsync(question, cancellationToken);
        }

        return Unit.Value;
    }
}
