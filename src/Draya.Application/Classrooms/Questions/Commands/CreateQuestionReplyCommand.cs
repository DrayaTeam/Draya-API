using Draya.Application.Classrooms.Questions.DTOs;
using Draya.Application.Classrooms.Questions.Notifications;
using Draya.Domain.Classrooms;
using MediatR;

namespace Draya.Application.Classrooms.Questions.Commands;

public record CreateQuestionReplyCommand(
    Guid QuestionId,
    Guid CurrentUserId,
    string Content
) : IRequest<QuestionReplyDto>;

public class CreateQuestionReplyCommandHandler : IRequestHandler<CreateQuestionReplyCommand, QuestionReplyDto>
{
    private readonly IQuestionRepository _questionRepository;
    private readonly IClassroomRepository _classroomRepository;
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly IMediator _mediator;

    public CreateQuestionReplyCommandHandler(
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

    public async Task<QuestionReplyDto> Handle(CreateQuestionReplyCommand request, CancellationToken cancellationToken)
    {
        var question = await _questionRepository.GetByIdAsync(request.QuestionId, cancellationToken);
        if (question == null)
            throw new KeyNotFoundException("Question not found.");

        var classroom = await _classroomRepository.GetByIdAsync(question.ClassroomId, cancellationToken);
        if (classroom == null)
            throw new UnauthorizedAccessException();

        bool isTeacher = classroom.TeacherId == request.CurrentUserId;
        if (!isTeacher)
        {
            var enrollment = await _enrollmentRepository.GetByStudentAndClassroomAsync(request.CurrentUserId, question.ClassroomId, cancellationToken);
            if (enrollment == null || enrollment.Status != EnrollmentStatus.Active)
                throw new UnauthorizedAccessException("Not enrolled in this classroom.");
        }

        if (isTeacher && question.HasTeacherAnswer)
        {
            throw new InvalidOperationException("This question already has an official teacher answer.");
        }

        var reply = new QuestionReply
        {
            QuestionId = request.QuestionId,
            AuthorId = request.CurrentUserId,
            Content = request.Content,
            IsTeacherAnswer = isTeacher
        };

        await _questionRepository.AddReplyAsync(reply, cancellationToken);

        // Update question counters
        question.ReplyCount++;
        if (isTeacher)
        {
            question.HasTeacherAnswer = true;
        }
        await _questionRepository.UpdateAsync(question, cancellationToken);

        var dto = new QuestionReplyDto(
            reply.Id,
            reply.QuestionId,
            reply.AuthorId,
            reply.Content,
            reply.CreatedAt,
            reply.IsTeacherAnswer,
            true
        );

        await _mediator.Publish(new QuestionRepliedNotification(
            question.ClassroomId,
            question.Id,
            reply.Id,
            reply.AuthorId,
            reply.Content,
            reply.CreatedAt,
            reply.IsTeacherAnswer
        ), cancellationToken);

        return dto;
    }
}
