using Draya.Domain.Identity.Exceptions;
using Draya.Domain.Classrooms;
using MediatR;

namespace Draya.Application.Classrooms.Questions.Commands;

public record EditQuestionReplyCommand(Guid ReplyId, Guid UserId, string Content, string? ImageUrl = null) : IRequest<Unit>;

public class EditQuestionReplyCommandHandler : IRequestHandler<EditQuestionReplyCommand, Unit>
{
    private readonly IQuestionRepository _questionRepository;

    public EditQuestionReplyCommandHandler(IQuestionRepository questionRepository)
    {
        _questionRepository = questionRepository;
    }

    public async Task<Unit> Handle(EditQuestionReplyCommand request, CancellationToken cancellationToken)
    {
        var reply = await _questionRepository.GetReplyByIdAsync(request.ReplyId, cancellationToken);
        if (reply == null)
            throw new NotFoundException($"Reply {request.ReplyId} not found.");

        if (reply.AuthorId != request.UserId)
            throw new UnauthorizedAccessException("You can only edit your own replies.");

        reply.Content = request.Content;
        reply.ImageUrl = request.ImageUrl; // Update ImageUrl (it will be null if not provided, allowing it to be cleared if desired)
        await _questionRepository.UpdateReplyAsync(reply, cancellationToken);

        return Unit.Value;
    }
}
