namespace Draya.Domain.Classrooms;

public class Classroom
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TeacherId { get; set; }
    public Guid SubjectId { get; set; }
    public Guid ClassroomTypeId { get; set; }
    public Guid GradeLevelId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string EnrollmentCode { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal Price { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public Subject? Subject { get; set; }
    public ClassroomType? ClassroomType { get; set; }
    public GradeLevel? GradeLevel { get; set; }
}
