using MediatR;

namespace Draya.Application.Classrooms.Questions.Notifications;

public record QuestionCreatedNotification(
    Guid ClassroomId,
    Guid QuestionId,
    Guid AuthorId,
    string Content,
    DateTime CreatedAt
) : INotification;

public record QuestionRepliedNotification(
    Guid ClassroomId,
    Guid QuestionId,
    Guid ReplyId,
    Guid AuthorId,
    string Content,
    DateTime CreatedAt,
    bool IsTeacherAnswer
) : INotification;

public record QuestionVoteUpdatedNotification(
    Guid ClassroomId,
    Guid QuestionId,
    int VoteCount
) : INotification;
