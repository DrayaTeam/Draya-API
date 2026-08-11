using Draya.Domain.Identity;
using Microsoft.AspNetCore.Identity;

namespace Draya.Infrastructure.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }

    // Navigation properties for 1:1 extension tables
    public Teacher? Teacher { get; set; }
    public Student? Student { get; set; }
    public PlatformAdmin? PlatformAdmin { get; set; }
}
