namespace Draya.Application.Classrooms.Feedback.DTOs;

public record ClassroomFeedbackDto(
    Guid FeedbackId,
    Guid ClassroomId,
    Guid StudentId,
    int Rating,
    string? Comment,
    DateTime CreatedAt
);
