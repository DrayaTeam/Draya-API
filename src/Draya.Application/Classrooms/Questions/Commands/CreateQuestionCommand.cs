using Draya.Application.Classrooms.Questions.DTOs;
using Draya.Application.Classrooms.Questions.Notifications;
using Draya.Domain.Classrooms;
using MediatR;

namespace Draya.Application.Classrooms.Questions.Commands;

public record CreateQuestionCommand(
    Guid ClassroomId,
    Guid CurrentUserId,
    string Content
) : IRequest<QuestionDto>;

public class CreateQuestionCommandHandler : IRequestHandler<CreateQuestionCommand, QuestionDto>
{
    private readonly IQuestionRepository _questionRepository;
    private readonly IClassroomRepository _classroomRepository;
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly IMediator _mediator;

    public CreateQuestionCommandHandler(
        IQuestionRepository questionRepository,
        IClassroomRepository classroomRepository,
        IEnrollmentRepository enrollmentRepository,
        IMediator mediator)
    {
        _questionRepository = questionRepository;
        _classroomRepository = classroomRepository;
        _enrollmentRepository = enrollmentRepository;
        _mediator = mediator;
    }

    public async Task<QuestionDto> Handle(CreateQuestionCommand request, CancellationToken cancellationToken)
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

        var question = new Question
        {
            ClassroomId = request.ClassroomId,
            AuthorId = request.CurrentUserId,
            Content = request.Content
        };

        await _questionRepository.AddAsync(question, cancellationToken);

        var dto = new QuestionDto(
            question.Id,
            question.ClassroomId,
            question.AuthorId,
            question.Content,
            question.CreatedAt,
            question.VoteCount,
            question.ReplyCount,
            question.HasTeacherAnswer,
            false,
            true
        );

        await _mediator.Publish(new QuestionCreatedNotification(
            question.ClassroomId,
            question.Id,
            question.AuthorId,
            question.Content,
            question.CreatedAt
        ), cancellationToken);

        return dto;
    }
}
