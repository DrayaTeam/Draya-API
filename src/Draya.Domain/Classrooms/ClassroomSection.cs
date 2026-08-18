namespace Draya.Domain.Classrooms;

using Draya.Domain.Materials;

public class ClassroomSection
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ClassroomId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Order { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Classroom? Classroom { get; set; }
    public ICollection<LearningMaterial> Materials { get; set; } = new List<LearningMaterial>();
}
