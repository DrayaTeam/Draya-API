namespace Draya.Domain.Materials;

public class LearningMaterial
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ClassroomId { get; set; }
    public string Title { get; set; } = string.Empty;
    public MaterialType MaterialType { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Soft delete flag
    public bool IsDeleted { get; set; } = false;

    // Navigation properties
    public ICollection<MaterialVersion> Versions { get; set; } = new List<MaterialVersion>();
}
