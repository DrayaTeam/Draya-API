using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Draya.Domain.Exams;
using MediatR;

namespace Draya.Application.Exams.Commands.Questions;

public record UpdateExamQuestionOptionDto(string Text, bool IsCorrect);

public class UpdateExamQuestionCommand : IRequest<bool>
{
    public Guid ExamId { get; set; }
    public Guid QuestionId { get; set; }
    public Guid TeacherId { get; set; }
    public string Text { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Difficulty { get; set; } = string.Empty;
    public string? Rubric { get; set; }
    public List<UpdateExamQuestionOptionDto>? Options { get; set; }
}

public class UpdateExamQuestionCommandHandler : IRequestHandler<UpdateExamQuestionCommand, bool>
{
    private readonly IExamRepository _examRepository;

    public UpdateExamQuestionCommandHandler(IExamRepository examRepository)
    {
        _examRepository = examRepository;
    }

    public async Task<bool> Handle(UpdateExamQuestionCommand request, CancellationToken cancellationToken)
    {
        var exam = await _examRepository.GetByIdAsync(request.ExamId, cancellationToken);
        if (exam == null) return false;

        var question = exam.Questions.FirstOrDefault(q => q.Id == request.QuestionId);
        if (question == null) return false;

        question.Update(request.Text, request.Type, request.Difficulty, request.Rubric);

        if (request.Options != null)
        {
            // For simplicity, we clear and re-add options. In EF Core, if options are properly configured,
            // clearing them should delete orphans or we might need to remove them via DbContext directly.
            // Since ExamQuestion options are part of aggregate root Exam, the repository handles save.
            question.ClearOptions();
            foreach (var opt in request.Options)
            {
                question.AddOption(new ExamQuestionOption(question.Id, opt.Text, opt.IsCorrect));
            }
        }

        await _examRepository.UpdateAsync(exam, cancellationToken);
        return true;
    }
}
