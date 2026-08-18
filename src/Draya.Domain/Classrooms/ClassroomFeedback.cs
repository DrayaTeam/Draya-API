namespace Draya.Domain.Classrooms;

public class ClassroomFeedback
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ClassroomId { get; set; }
    public Guid StudentId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Classroom? Classroom { get; set; }
}
