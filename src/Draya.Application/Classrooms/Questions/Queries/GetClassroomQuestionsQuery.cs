using Draya.Application.Common.Models;
using Draya.Application.Classrooms.Questions.DTOs;
using Draya.Domain.Classrooms;
using Draya.Domain.Identity;
using MediatR;

namespace Draya.Application.Classrooms.Questions.Queries;

public record GetClassroomQuestionsQuery(
    Guid ClassroomId,
    Guid CurrentUserId,
    string CurrentUserRole,
    string SortBy,
    string FilterBy,
    int Page,
    int PageSize
) : IRequest<PaginatedResult<QuestionDto>>;

public class GetClassroomQuestionsQueryHandler : IRequestHandler<GetClassroomQuestionsQuery, PaginatedResult<QuestionDto>>
{
    private readonly IQuestionRepository _questionRepository;
    private readonly IClassroomRepository _classroomRepository;
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly ITeacherRepository _teacherRepository;
    private readonly IStudentRepository _studentRepository;

    public GetClassroomQuestionsQueryHandler(
        IQuestionRepository questionRepository,
        IClassroomRepository classroomRepository,
        IEnrollmentRepository enrollmentRepository,
        ITeacherRepository teacherRepository,
        IStudentRepository studentRepository)
    {
        _questionRepository = questionRepository;
        _classroomRepository = classroomRepository;
        _enrollmentRepository = enrollmentRepository;
        _teacherRepository = teacherRepository;
        _studentRepository = studentRepository;
    }

    public async Task<PaginatedResult<QuestionDto>> Handle(GetClassroomQuestionsQuery request, CancellationToken cancellationToken)
    {
        var classroom = await _classroomRepository.GetByIdAsync(request.ClassroomId, cancellationToken);
        if (classroom == null)
            throw new UnauthorizedAccessException("Classroom not found.");

        if (classroom.TeacherId != request.CurrentUserId)
        {
            var enrollment = await _enrollmentRepository.GetByStudentAndClassroomAsync(request.CurrentUserId, request.ClassroomId, cancellationToken);
            if (enrollment == null || enrollment.Status != EnrollmentStatus.Active)
                throw new UnauthorizedAccessException("Not enrolled in this classroom.");
        }

        var (items, count) = await _questionRepository.GetClassroomQuestionsAsync(
            request.ClassroomId,
            request.SortBy,
            request.FilterBy,
            request.CurrentUserId,
            request.Page,
            request.PageSize,
            cancellationToken);

        var authorIds = items.Select(q => q.AuthorId).Distinct().ToList();
        var teachers = (await _teacherRepository.GetByUserIdsAsync(authorIds, cancellationToken)).ToDictionary(t => t.UserId);
        var students = (await _studentRepository.GetByUserIdsAsync(authorIds, cancellationToken)).ToDictionary(s => s.UserId);

        var dtos = new List<QuestionDto>();
        foreach (var q in items)
        {
            var hasVoted = await _questionRepository.HasUserVotedAsync(q.Id, request.CurrentUserId, cancellationToken);

            string authorName = "User";
            string authorRole = "User";
            string? authorProfilePictureUrl = null;

            if (teachers.TryGetValue(q.AuthorId, out var teacher))
            {
                authorName = teacher.FullName;
                authorRole = "Teacher";
                authorProfilePictureUrl = teacher.ProfilePictureUrl;
            }
            else if (students.TryGetValue(q.AuthorId, out var student))
            {
                authorName = student.FullName;
                authorRole = "Student";
                authorProfilePictureUrl = student.ProfilePictureUrl;
            }

            dtos.Add(new QuestionDto(
                q.Id,
                q.ClassroomId,
                q.AuthorId,
                authorName,
                authorRole,
                authorProfilePictureUrl,
                q.Content,
                q.ImageUrl,
                q.CreatedAt,
                q.VoteCount,
                q.ReplyCount,
                q.HasTeacherAnswer,
                hasVoted,
                q.AuthorId == request.CurrentUserId
            ));
        }

        return new PaginatedResult<QuestionDto>(dtos, count, request.Page, request.PageSize);
    }
}
