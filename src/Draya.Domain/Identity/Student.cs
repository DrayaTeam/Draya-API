namespace Draya.Domain.Identity;

public class Student
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string ParentGuardianEmail { get; set; } = string.Empty;
    public string ParentGuardianName { get; set; } = string.Empty;
    public string ParentGuardianPhone { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastActivityDate { get; set; }
    public int CurrentStreak { get; set; }
}


