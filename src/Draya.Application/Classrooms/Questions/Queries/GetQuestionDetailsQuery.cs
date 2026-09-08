using Draya.Application.Classrooms.Questions.DTOs;
using Draya.Domain.Classrooms;
using Draya.Domain.Identity;
using MediatR;

namespace Draya.Application.Classrooms.Questions.Queries;

public record GetQuestionDetailsQuery(
    Guid QuestionId,
    Guid CurrentUserId
) : IRequest<QuestionDetailsDto>;

public record QuestionDetailsDto(
    QuestionDto Question,
    List<QuestionReplyDto> Replies
);

public class GetQuestionDetailsQueryHandler : IRequestHandler<GetQuestionDetailsQuery, QuestionDetailsDto>
{
    private readonly IQuestionRepository _questionRepository;
    private readonly IClassroomRepository _classroomRepository;
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly ITeacherRepository _teacherRepository;
    private readonly IStudentRepository _studentRepository;

    public GetQuestionDetailsQueryHandler(
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

    public async Task<QuestionDetailsDto> Handle(GetQuestionDetailsQuery request, CancellationToken cancellationToken)
    {
        var question = await _questionRepository.GetByIdAsync(request.QuestionId, cancellationToken);
        if (question == null)
            throw new KeyNotFoundException("Question not found.");

        var classroom = await _classroomRepository.GetByIdAsync(question.ClassroomId, cancellationToken);
        if (classroom == null)
            throw new UnauthorizedAccessException();

        if (classroom.TeacherId != request.CurrentUserId)
        {
            var enrollment = await _enrollmentRepository.GetByStudentAndClassroomAsync(request.CurrentUserId, question.ClassroomId, cancellationToken);
            if (enrollment == null || enrollment.Status != EnrollmentStatus.Active)
                throw new UnauthorizedAccessException("Not enrolled in this classroom.");
        }

        var authorIds = question.Replies.Select(r => r.AuthorId).Append(question.AuthorId).Distinct().ToList();
        var teachers = (await _teacherRepository.GetByUserIdsAsync(authorIds, cancellationToken)).ToDictionary(t => t.UserId);
        var students = (await _studentRepository.GetByUserIdsAsync(authorIds, cancellationToken)).ToDictionary(s => s.UserId);

        (string name, string role, string? avatar) GetAuthorInfo(Guid authorId)
        {
            if (teachers.TryGetValue(authorId, out var teacher))
                return (teacher.FullName, "Teacher", teacher.ProfilePictureUrl);
            if (students.TryGetValue(authorId, out var student))
                return (student.FullName, "Student", student.ProfilePictureUrl);
            return ("User", "User", null);
        }

        var hasVoted = await _questionRepository.HasUserVotedAsync(question.Id, request.CurrentUserId, cancellationToken);
        var (qAuthorName, qAuthorRole, qAuthorAvatar) = GetAuthorInfo(question.AuthorId);

        var questionDto = new QuestionDto(
            question.Id,
            question.ClassroomId,
            question.AuthorId,
            qAuthorName,
            qAuthorRole,
            qAuthorAvatar,
            question.Content,
            question.ImageUrl,
            question.CreatedAt,
            question.VoteCount,
            question.ReplyCount,
            question.HasTeacherAnswer,
            hasVoted,
            question.AuthorId == request.CurrentUserId
        );

        var replies = question.Replies.Select(r =>
        {
            var (rAuthorName, rAuthorRole, rAuthorAvatar) = GetAuthorInfo(r.AuthorId);
            return new QuestionReplyDto(
                r.Id,
                r.QuestionId,
                r.AuthorId,
                rAuthorName,
                rAuthorRole,
                rAuthorAvatar,
                r.Content,
                r.ImageUrl,
                r.CreatedAt,
                r.IsTeacherAnswer,
                r.AuthorId == request.CurrentUserId
            );
        }).ToList();

        return new QuestionDetailsDto(questionDto, replies);
    }
}
