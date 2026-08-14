using Draya.Application.Common.Models;
using Draya.Application.Classrooms.Questions.DTOs;
using Draya.Domain.Classrooms;
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

    public GetClassroomQuestionsQueryHandler(
        IQuestionRepository questionRepository,
        IClassroomRepository classroomRepository,
        IEnrollmentRepository enrollmentRepository)
    {
        _questionRepository = questionRepository;
        _classroomRepository = classroomRepository;
        _enrollmentRepository = enrollmentRepository;
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

        var dtos = new List<QuestionDto>();
        foreach (var q in items)
        {
            var hasVoted = await _questionRepository.HasUserVotedAsync(q.Id, request.CurrentUserId, cancellationToken);
            dtos.Add(new QuestionDto(
                q.Id,
                q.ClassroomId,
                q.AuthorId,
                q.Content,
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
