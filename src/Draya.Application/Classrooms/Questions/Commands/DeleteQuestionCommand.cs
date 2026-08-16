using Draya.Domain.Identity.Exceptions;
using Draya.Domain.Classrooms;
using MediatR;

namespace Draya.Application.Classrooms.Questions.Commands;

public record DeleteQuestionCommand(Guid QuestionId, Guid UserId) : IRequest<Unit>;

public class DeleteQuestionCommandHandler : IRequestHandler<DeleteQuestionCommand, Unit>
{
    private readonly IQuestionRepository _questionRepository;

    public DeleteQuestionCommandHandler(IQuestionRepository questionRepository)
    {
        _questionRepository = questionRepository;
    }

    public async Task<Unit> Handle(DeleteQuestionCommand request, CancellationToken cancellationToken)
    {
        var question = await _questionRepository.GetByIdAsync(request.QuestionId, cancellationToken);
        if (question == null)
            throw new NotFoundException($"Question {request.QuestionId} not found.");

        if (question.AuthorId != request.UserId)
            throw new UnauthorizedAccessException("You can only delete your own questions.");

        await _questionRepository.DeleteAsync(question, cancellationToken);

        return Unit.Value;
    }
}
