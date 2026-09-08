namespace Draya.Domain.Identity;

public class Teacher
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Specialization { get; set; }
    public string? Description { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

