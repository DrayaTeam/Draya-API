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
            // Snapshot the current options before clearing them from the aggregate.
            var oldOptions = question.Options.ToList();

            // Build the replacement options.
            var newOptions = request.Options
                .Select(o => new ExamQuestionOption(question.Id, o.Text, o.IsCorrect))
                .ToList();

            // Stage the swap in EF's change tracker:
            //   oldOptions → Deleted (will be DELETEd)
            //   newOptions → Added   (will be INSERTed)
            // Both are flushed to the DB in the single SaveChangesAsync inside UpdateAsync.
            _examRepository.ReplaceOptionsForQuestion(oldOptions, newOptions);

            // Sync the in-memory aggregate collection to match.
            question.ClearOptions();
            foreach (var opt in newOptions)
                question.AddOption(opt);
        }

        await _examRepository.UpdateAsync(exam, cancellationToken);
        return true;
    }
}
