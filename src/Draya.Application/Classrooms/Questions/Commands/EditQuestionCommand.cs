using Draya.Domain.Identity.Exceptions;
using Draya.Domain.Classrooms;
using MediatR;

namespace Draya.Application.Classrooms.Questions.Commands;

public record EditQuestionCommand(Guid QuestionId, Guid UserId, string Content) : IRequest<Unit>;

public class EditQuestionCommandHandler : IRequestHandler<EditQuestionCommand, Unit>
{
    private readonly IQuestionRepository _questionRepository;

    public EditQuestionCommandHandler(IQuestionRepository questionRepository)
    {
        _questionRepository = questionRepository;
    }

    public async Task<Unit> Handle(EditQuestionCommand request, CancellationToken cancellationToken)
    {
        var question = await _questionRepository.GetByIdAsync(request.QuestionId, cancellationToken);
        if (question == null)
            throw new NotFoundException($"Question {request.QuestionId} not found.");

        if (question.AuthorId != request.UserId)
            throw new UnauthorizedAccessException("You can only edit your own questions.");

        question.Content = request.Content;
        await _questionRepository.UpdateAsync(question, cancellationToken);

        return Unit.Value;
    }
}
