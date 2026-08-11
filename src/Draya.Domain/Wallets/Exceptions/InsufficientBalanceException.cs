namespace Draya.Domain.Wallets.Exceptions;

public class InsufficientBalanceException : Exception
{
    public InsufficientBalanceException(string message = "Insufficient wallet balance to perform this operation.") 
        : base(message)
    {
    }
}
