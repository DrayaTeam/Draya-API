namespace Draya.Domain.Identity;

public class PlatformAdmin
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
