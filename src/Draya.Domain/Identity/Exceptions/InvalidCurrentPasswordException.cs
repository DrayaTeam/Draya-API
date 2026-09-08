namespace Draya.Domain.Identity.Exceptions;

public class InvalidCurrentPasswordException : Exception
{
    public InvalidCurrentPasswordException()
        : base("Current password is incorrect.")
    {
    }

    public InvalidCurrentPasswordException(string message)
        : base(message)
    {
    }
}
