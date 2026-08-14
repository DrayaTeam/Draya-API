namespace Draya.Application.Classrooms.Questions.DTOs;

public record QuestionDto(
    Guid Id,
    Guid ClassroomId,
    Guid AuthorId,
    string Content,
    DateTime CreatedAt,
    int VoteCount,
    int ReplyCount,
    bool HasTeacherAnswer,
    bool HasVoted,
    bool IsAuthor
);
