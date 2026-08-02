namespace Draya.Application.Identity.DTOs;

public record UserSummaryDto(
    Guid UserId,
    string FullName,
    string Role
);
