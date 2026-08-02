namespace Draya.Domain.Identity.Exceptions;

public class DuplicateEmailException : Exception
{
    public string Email { get; }

    public DuplicateEmailException(string email)
        : base($"An account with email '{email}' already exists.")
    {
        Email = email;
    }
}
