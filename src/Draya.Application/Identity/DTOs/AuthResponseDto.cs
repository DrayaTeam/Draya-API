namespace Draya.Application.Identity.DTOs;

public record AuthResponseDto(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn,
    UserSummaryDto User
);
