using Draya.Application.Classrooms.Feedback.DTOs;
using Draya.Domain.Classrooms;
using Draya.Domain.Classrooms.Exceptions;
using MediatR;

namespace Draya.Application.Classrooms.Feedback.Commands;

public class SubmitClassroomFeedbackCommandHandler : IRequestHandler<SubmitClassroomFeedbackCommand, ClassroomFeedbackDto>
{
    private readonly IClassroomRepository _classroomRepository;
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly IClassroomFeedbackRepository _feedbackRepository;

    public SubmitClassroomFeedbackCommandHandler(
        IClassroomRepository classroomRepository,
        IEnrollmentRepository enrollmentRepository,
        IClassroomFeedbackRepository feedbackRepository)
    {
        _classroomRepository = classroomRepository;
        _enrollmentRepository = enrollmentRepository;
        _feedbackRepository = feedbackRepository;
    }

    public async Task<ClassroomFeedbackDto> Handle(SubmitClassroomFeedbackCommand request, CancellationToken cancellationToken)
    {
        var classroom = await _classroomRepository.GetByIdAsync(request.ClassroomId, cancellationToken);
        if (classroom is null)
        {
            throw new ClassroomNotFoundException();
        }

        var enrollment = await _enrollmentRepository.GetByStudentAndClassroomAsync(
            request.StudentId,
            request.ClassroomId,
            cancellationToken);

        if (enrollment is null || enrollment.Status != EnrollmentStatus.Active)
        {
            throw new ClassroomFeedbackRequiresEnrollmentException();
        }

        if (await _feedbackRepository.ExistsAsync(request.ClassroomId, request.StudentId, cancellationToken))
        {
            throw new ClassroomFeedbackAlreadySubmittedException();
        }

        var feedback = new ClassroomFeedback
        {
            ClassroomId = request.ClassroomId,
            StudentId = request.StudentId,
            Rating = request.Rating,
            Comment = string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        await _feedbackRepository.AddAsync(feedback, cancellationToken);
        await _feedbackRepository.SaveChangesAsync(cancellationToken);

        return new ClassroomFeedbackDto(
            feedback.Id,
            feedback.ClassroomId,
            feedback.StudentId,
            feedback.Rating,
            feedback.Comment,
            feedback.CreatedAt);
    }
}
