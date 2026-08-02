namespace Draya.Domain.Identity;

public class Student
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string ParentGuardianEmail { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public AppUser AppUser { get; set; } = null!;
}
