using Draya.Domain.Identity;

namespace Draya.Application.Common.Interfaces;

public interface ITokenService
{
    (string Token, int ExpiresInSeconds) GenerateAccessToken(AppUser user, string fullName);
    string GenerateRefreshToken();
    string HashRefreshToken(string refreshToken);
    Guid? GetUserIdFromAccessToken(string token);
}
