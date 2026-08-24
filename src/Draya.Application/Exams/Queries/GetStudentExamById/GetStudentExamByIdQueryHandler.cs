using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Draya.Application.Exams.DTOs;
using Draya.Domain.Classrooms;
using Draya.Domain.Exams;
using Draya.Domain.Identity;
using MediatR;



namespace Draya.Application.Exams.Queries.GetStudentExamById;

public class GetStudentExamByIdQueryHandler : IRequestHandler<GetStudentExamByIdQuery, StudentExamDto?>
{
    private readonly IExamRepository _examRepository;
    private readonly IClassroomRepository _classroomRepository;
    private readonly ITeacherRepository _teacherRepository;

    public GetStudentExamByIdQueryHandler(
        IExamRepository examRepository,
        IClassroomRepository classroomRepository,
        ITeacherRepository teacherRepository)
    {
        _examRepository = examRepository;
        _classroomRepository = classroomRepository;
        _teacherRepository = teacherRepository;
    }

    public async Task<StudentExamDto?> Handle(GetStudentExamByIdQuery request, CancellationToken cancellationToken)
    {
        var exam = await _examRepository.GetByIdAsync(request.Id, cancellationToken);
        if (exam == null) return null;

        var classroom = await _classroomRepository.GetByIdAsync(exam.ClassroomId, cancellationToken);
        var teacher = classroom != null ? await _teacherRepository.GetByUserIdAsync(classroom.TeacherId, cancellationToken) : null;
        string teacherName = teacher?.FullName ?? "Unknown";

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
            teacherName,
            exam.Questions.Select(q => new StudentExamQuestionDto(
                q.Id,
                q.Text,
                q.Type,
                q.Difficulty,
                q.Options.Select(o => new StudentExamQuestionOptionDto(
                    o.Id,
                    o.Text
                )).ToList()
            )).ToList()
        );
    }
}
