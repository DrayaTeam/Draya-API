using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Draya.Domain.Exams;
using MediatR;

namespace Draya.Application.Exams.Commands.Questions;

public class AddExamQuestionCommand : IRequest<Guid?>
{
    public Guid ExamId { get; set; }
    public Guid TeacherId { get; set; }
    public string Text { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Difficulty { get; set; } = string.Empty;
    public string? Rubric { get; set; }
    public string SourceChunkIds { get; set; } = string.Empty;
    public List<UpdateExamQuestionOptionDto>? Options { get; set; }
}

public class AddExamQuestionCommandHandler : IRequestHandler<AddExamQuestionCommand, Guid?>
{
    private readonly IExamRepository _examRepository;

    public AddExamQuestionCommandHandler(IExamRepository examRepository)
    {
        _examRepository = examRepository;
    }

    public async Task<Guid?> Handle(AddExamQuestionCommand request, CancellationToken cancellationToken)
    {
        var exam = await _examRepository.GetByIdAsync(request.ExamId, cancellationToken);
        if (exam == null) return null;

        var question = new ExamQuestion(
            exam.Id,
            request.Text,
            request.Type,
            request.Difficulty,
            request.SourceChunkIds,
            request.Rubric
        );

        if (request.Options != null)
        {
            foreach (var opt in request.Options)
            {
                question.AddOption(new ExamQuestionOption(question.Id, opt.Text, opt.IsCorrect));
            }
        }

        exam.AddQuestion(question);
        await _examRepository.UpdateAsync(exam, cancellationToken);

        return question.Id;
    }
}
