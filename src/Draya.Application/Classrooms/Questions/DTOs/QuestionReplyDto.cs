namespace Draya.Application.Classrooms.Questions.DTOs;

public record QuestionReplyDto(
    Guid Id,
    Guid QuestionId,
    Guid AuthorId,
    string AuthorName,
    string AuthorRole,
    string? AuthorProfilePictureUrl,
    string Content,
    string? ImageUrl,
    DateTime CreatedAt,
    bool IsTeacherAnswer,
    bool IsAuthor
);

