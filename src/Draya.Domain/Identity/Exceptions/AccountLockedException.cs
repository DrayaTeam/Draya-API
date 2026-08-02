namespace Draya.Domain.Identity.Exceptions;

public class AccountLockedException : Exception
{
    public AccountLockedException() : base("Account is temporarily locked.") { }
}
