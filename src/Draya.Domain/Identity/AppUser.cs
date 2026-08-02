namespace Draya.Domain.Identity;

public class AppUser
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public Role Role { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    public int FailedLoginAttempts { get; set; }
    public DateTime? LockoutEndUtc { get; set; }

    // Navigation properties for 1:1 extension tables
    public Teacher? Teacher { get; set; }
    public Student? Student { get; set; }
    public PlatformAdmin? PlatformAdmin { get; set; }
}
