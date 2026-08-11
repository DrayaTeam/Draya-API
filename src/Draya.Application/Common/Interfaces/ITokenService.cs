namespace Draya.Application.Common.Interfaces;

public interface ITokenService
{
    (string Token, int ExpiresInSeconds) GenerateAccessToken(Guid userId, string email, string role, string fullName);
    string GenerateRefreshToken();
    string HashRefreshToken(string refreshToken);
    Guid? GetUserIdFromAccessToken(string token);
}
