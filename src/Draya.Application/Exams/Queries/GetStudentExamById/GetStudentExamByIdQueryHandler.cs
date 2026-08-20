using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Draya.Application.Exams.DTOs;
using Draya.Domain.Exams;
using MediatR;

namespace Draya.Application.Exams.Queries.GetStudentExamById;

public class GetStudentExamByIdQueryHandler : IRequestHandler<GetStudentExamByIdQuery, StudentExamDto?>
{
    private readonly IExamRepository _examRepository;

    public GetStudentExamByIdQueryHandler(IExamRepository examRepository)
    {
        _examRepository = examRepository;
    }

    public async Task<StudentExamDto?> Handle(GetStudentExamByIdQuery request, CancellationToken cancellationToken)
    {
        var exam = await _examRepository.GetByIdAsync(request.Id, cancellationToken);
        if (exam == null) return null;

        return new StudentExamDto(
            exam.Id,
            exam.ClassroomId,
            exam.SectionId,
            exam.Title,
            exam.Topic,
            exam.DurationMinutes,
            exam.StartDate,
            exam.EndDate,
            exam.AllowedAttempts,
            exam.CreatedAt,
            exam.Questions.Select(q => new StudentExamQuestionDto(
                q.Id,
                q.Text,
                q.Type,
                q.Difficulty,
                q.SourceChunkIds,
                q.Rubric,
                q.Options.Select(o => new StudentExamQuestionOptionDto(
                    o.Id,
                    o.Text
                )).ToList()
            )).ToList()
        );
    }
}
