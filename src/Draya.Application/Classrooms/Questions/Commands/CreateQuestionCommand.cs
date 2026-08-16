using Draya.Application.Classrooms.Questions.DTOs;
using Draya.Application.Classrooms.Questions.Notifications;
using Draya.Application.Materials;
using Draya.Domain.Classrooms;
using Draya.Domain.Identity;
using MediatR;

namespace Draya.Application.Classrooms.Questions.Commands;

public record CreateQuestionCommand(
    Guid ClassroomId,
    Guid CurrentUserId,
    string Content,
    Stream? ImageStream = null,
    string? ImageFileName = null,
    string? ImageContentType = null,
    string? ImageUrl = null
) : IRequest<QuestionDto>;

public class CreateQuestionCommandHandler : IRequestHandler<CreateQuestionCommand, QuestionDto>
{
    private readonly IQuestionRepository _questionRepository;
    private readonly IClassroomRepository _classroomRepository;
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly ITeacherRepository _teacherRepository;
    private readonly IStudentRepository _studentRepository;
    private readonly IMediaStorageService _mediaStorageService;
    private readonly IMediator _mediator;

    public CreateQuestionCommandHandler(
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

        string? resolvedImageUrl = request.ImageUrl;
        if (request.ImageStream != null && !string.IsNullOrEmpty(request.ImageFileName) && !string.IsNullOrEmpty(request.ImageContentType))
        {
            var assetPath = $"Questions/{request.ClassroomId}/{Guid.NewGuid()}_{request.ImageFileName}";
            var metadata = await _mediaStorageService.UploadAsync(request.ImageStream, assetPath, request.ImageContentType, cancellationToken);
            resolvedImageUrl = await _mediaStorageService.GetSecureDeliveryUrlAsync(metadata);
        }

        var question = new Question
        {
            ClassroomId = request.ClassroomId,
            AuthorId = request.CurrentUserId,
            Content = request.Content,
            ImageUrl = resolvedImageUrl
        };

        await _questionRepository.AddAsync(question, cancellationToken);

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

        var dto = new QuestionDto(
            question.Id,
            question.ClassroomId,
            question.AuthorId,
            authorName,
            authorRole,
            authorProfilePictureUrl,
            question.Content,
            question.ImageUrl,
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
            authorName,
            authorRole,
            authorProfilePictureUrl,
            question.Content,
            question.ImageUrl,
            question.CreatedAt
        ), cancellationToken);

        return dto;
    }
}
