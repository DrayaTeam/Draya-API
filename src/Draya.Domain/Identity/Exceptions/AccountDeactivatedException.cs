namespace Draya.Domain.Identity.Exceptions;

public class AccountDeactivatedException : Exception
{
    public AccountDeactivatedException()
        : base("Your account has been deactivated. Please contact support.")
    {
    }
}
