namespace Draya.Application.Classrooms.Questions.DTOs;

public record QuestionReplyDto(
    Guid Id,
    Guid QuestionId,
    Guid AuthorId,
    string Content,
    DateTime CreatedAt,
    bool IsTeacherAnswer,
    bool IsAuthor
);
