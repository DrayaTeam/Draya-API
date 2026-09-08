using Draya.Application.Classrooms.Questions.DTOs;
using Draya.Application.Classrooms.Questions.Notifications;
using Draya.Application.Materials;
using Draya.Domain.Classrooms;
using Draya.Domain.Identity;
using MediatR;

namespace Draya.Application.Classrooms.Questions.Commands;

public record CreateQuestionReplyCommand(
    Guid QuestionId,
    Guid CurrentUserId,
    string Content,
    Stream? ImageStream = null,
    string? ImageFileName = null,
    string? ImageContentType = null,
    string? ImageUrl = null
) : IRequest<QuestionReplyDto>;

public class CreateQuestionReplyCommandHandler : IRequestHandler<CreateQuestionReplyCommand, QuestionReplyDto>
{
    private readonly IQuestionRepository _questionRepository;
    private readonly IClassroomRepository _classroomRepository;
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly ITeacherRepository _teacherRepository;
    private readonly IStudentRepository _studentRepository;
    private readonly IMediaStorageService _mediaStorageService;
    private readonly IMediator _mediator;

    public CreateQuestionReplyCommandHandler(
        IQuestionRepository questionRepository,
        IClassroomRepository classroomRepository,
        IEnrollmentRepository enrollmentRepository,
        ITeacherRepository teacherRepository,
        IStudentRepository studentRepository,
        IMediaStorageService mediaStorageService,
        IMediator mediator)
    {
        _questionRepository = questionRepository;
        _classroomRepository = classroomRepository;
        _enrollmentRepository = enrollmentRepository;
        _teacherRepository = teacherRepository;
        _studentRepository = studentRepository;
        _mediaStorageService = mediaStorageService;
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


        string? resolvedImageUrl = request.ImageUrl;
        if (request.ImageStream != null && !string.IsNullOrEmpty(request.ImageFileName) && !string.IsNullOrEmpty(request.ImageContentType))
        {
            var assetPath = $"Questions/Replies/{question.Id}/{Guid.NewGuid()}_{request.ImageFileName}";
            var metadata = await _mediaStorageService.UploadAsync(request.ImageStream, assetPath, request.ImageContentType, cancellationToken);
            resolvedImageUrl = await _mediaStorageService.GetSecureDeliveryUrlAsync(metadata);
        }

        var reply = new QuestionReply
        {
            QuestionId = request.QuestionId,
            AuthorId = request.CurrentUserId,
            Content = request.Content,
            ImageUrl = resolvedImageUrl,
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

        string authorName = "User";
        string authorRole = "User";
        string? authorProfilePictureUrl = null;

        var teacher = await _teacherRepository.GetByUserIdAsync(request.CurrentUserId, cancellationToken);
        if (teacher != null)
        {
            authorName = teacher.FullName;
            authorRole = "Teacher";
            authorProfilePictureUrl = teacher.ProfilePictureUrl;
        }
        else
        {
            var student = await _studentRepository.GetByUserIdAsync(request.CurrentUserId, cancellationToken);
            if (student != null)
            {
                authorName = student.FullName;
                authorRole = "Student";
                authorProfilePictureUrl = student.ProfilePictureUrl;
            }
        }

        var dto = new QuestionReplyDto(
            reply.Id,
            reply.QuestionId,
            reply.AuthorId,
            authorName,
            authorRole,
            authorProfilePictureUrl,
            reply.Content,
            reply.ImageUrl,
            reply.CreatedAt,
            reply.IsTeacherAnswer,
            true
        );

        await _mediator.Publish(new QuestionRepliedNotification(
            question.ClassroomId,
            question.Id,
            reply.Id,
            reply.AuthorId,
            authorName,
            authorRole,
            authorProfilePictureUrl,
            reply.Content,
            reply.ImageUrl,
            reply.CreatedAt,
            reply.IsTeacherAnswer
        ), cancellationToken);

        return dto;
    }
}
