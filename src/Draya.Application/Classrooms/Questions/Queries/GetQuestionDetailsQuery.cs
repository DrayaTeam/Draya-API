using Draya.Application.Classrooms.Questions.DTOs;
using Draya.Domain.Classrooms;
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

    public GetQuestionDetailsQueryHandler(
        IQuestionRepository questionRepository,
        IClassroomRepository classroomRepository,
        IEnrollmentRepository enrollmentRepository)
    {
        _questionRepository = questionRepository;
        _classroomRepository = classroomRepository;
        _enrollmentRepository = enrollmentRepository;
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

        var hasVoted = await _questionRepository.HasUserVotedAsync(question.Id, request.CurrentUserId, cancellationToken);
        
        var questionDto = new QuestionDto(
            question.Id,
            question.ClassroomId,
            question.AuthorId,
            question.Content,
            question.CreatedAt,
            question.VoteCount,
            question.ReplyCount,
            question.HasTeacherAnswer,
            hasVoted,
            question.AuthorId == request.CurrentUserId
        );

        var replies = question.Replies.Select(r => new QuestionReplyDto(
            r.Id,
            r.QuestionId,
            r.AuthorId,
            r.Content,
            r.CreatedAt,
            r.IsTeacherAnswer,
            r.AuthorId == request.CurrentUserId
        )).ToList();

        return new QuestionDetailsDto(questionDto, replies);
    }
}
