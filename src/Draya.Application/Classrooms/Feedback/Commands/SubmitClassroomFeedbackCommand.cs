using Draya.Application.Classrooms.Feedback.DTOs;
using MediatR;

namespace Draya.Application.Classrooms.Feedback.Commands;

public record SubmitClassroomFeedbackCommand(
    Guid ClassroomId,
    Guid StudentId,
    int Rating,
    string? Comment
) : IRequest<ClassroomFeedbackDto>;
