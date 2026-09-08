namespace Draya.Application.Identity.DTOs;

public record SupervisorDto(
    Guid Id,
    string Name,
    string Email,
    string Role,
    bool IsActive,
    string Status,
    DateTime? InvitedAt,
    DateTime CreatedAt
);
