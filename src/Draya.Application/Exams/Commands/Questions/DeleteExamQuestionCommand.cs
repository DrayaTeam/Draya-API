using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Draya.Domain.Exams;
using MediatR;

namespace Draya.Application.Exams.Commands.Questions;

public class DeleteExamQuestionCommand : IRequest<bool>
{
    public Guid ExamId { get; set; }
    public Guid QuestionId { get; set; }
    public Guid TeacherId { get; set; }
}

public class DeleteExamQuestionCommandHandler : IRequestHandler<DeleteExamQuestionCommand, bool>
{
    private readonly IExamRepository _examRepository;

    public DeleteExamQuestionCommandHandler(IExamRepository examRepository)
    {
        _examRepository = examRepository;
    }

    public async Task<bool> Handle(DeleteExamQuestionCommand request, CancellationToken cancellationToken)
    {
        var exam = await _examRepository.GetByIdAsync(request.ExamId, cancellationToken);
        if (exam == null) return false;

        var question = exam.Questions.FirstOrDefault(q => q.Id == request.QuestionId);
        if (question == null) return false;

        exam.RemoveQuestion(question.Id);

        await _examRepository.UpdateAsync(exam, cancellationToken);
        return true;
    }
}
