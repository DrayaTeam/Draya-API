namespace Draya.Domain.Identity.Exceptions;

public class InvalidPasswordResetTokenException : Exception
{
    public InvalidPasswordResetTokenException() : base("The password reset token is invalid, expired, or has already been used.") { }
}
