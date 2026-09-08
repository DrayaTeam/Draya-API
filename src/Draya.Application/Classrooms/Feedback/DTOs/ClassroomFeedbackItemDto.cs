namespace Draya.Application.Classrooms.Feedback.DTOs;

public record ClassroomFeedbackItemDto(
    Guid FeedbackId,
    string StudentName,
    string? StudentAvatarUrl,
    int Rating,
    string? Comment,
    DateTime CreatedAt
);
