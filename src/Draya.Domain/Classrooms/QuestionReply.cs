namespace Draya.Domain.Classrooms;

public class QuestionReply
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid QuestionId { get; set; }
    public Guid AuthorId { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // A question can have at most one official teacher answer.
    // Only the classroom teacher can create a reply with IsTeacherAnswer = true.
    public bool IsTeacherAnswer { get; set; } = false;

    public Question? Question { get; set; }
}
