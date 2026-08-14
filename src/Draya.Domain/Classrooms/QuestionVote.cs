namespace Draya.Domain.Classrooms;

public class QuestionVote
{
    public Guid QuestionId { get; set; }
    public Guid UserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Question? Question { get; set; }
}
