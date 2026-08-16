namespace Draya.Domain.Classrooms;

public class Question
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ClassroomId { get; set; }
    public Guid AuthorId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int VoteCount { get; set; } = 0;
    public int ReplyCount { get; set; } = 0;
    public bool HasTeacherAnswer { get; set; } = false;

    public Classroom? Classroom { get; set; }
    public ICollection<QuestionReply> Replies { get; set; } = new List<QuestionReply>();
    public ICollection<QuestionVote> Votes { get; set; } = new List<QuestionVote>();
}
