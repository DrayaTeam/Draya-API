namespace Draya.Domain.Identity.Exceptions;

public class InvalidRefreshTokenException : Exception
{
    public InvalidRefreshTokenException()
        : base("Invalid, expired, or revoked refresh token.")
    {
    }
}
