using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Draya.Application.Exams.DTOs;
using Draya.Domain.Exams;
using MediatR;

namespace Draya.Application.Exams.Queries.GetExamById;

public class GetExamByIdQueryHandler : IRequestHandler<GetExamByIdQuery, ExamDto?>
{
    private readonly IExamRepository _examRepository;

    public GetExamByIdQueryHandler(IExamRepository examRepository)
    {
        _examRepository = examRepository;
    }

    public async Task<ExamDto?> Handle(GetExamByIdQuery request, CancellationToken cancellationToken)
    {
        var exam = await _examRepository.GetByIdAsync(request.Id, cancellationToken);
        if (exam == null) return null;

        return new ExamDto(
            exam.Id,
            exam.ClassroomId,
            exam.SectionId,
            exam.Title,
            exam.Topic,
            exam.CreatedAt,
            exam.Questions.Select(q => new ExamQuestionDto(
                q.Id,
                q.Text,
                q.Type,
                q.Difficulty,
                q.SourceChunkIds,
                q.Rubric,
                q.Options.Select(o => new ExamQuestionOptionDto(
                    o.Id,
                    o.Text,
                    o.IsCorrect
                )).ToList()
            )).ToList()
        );
    }
}
