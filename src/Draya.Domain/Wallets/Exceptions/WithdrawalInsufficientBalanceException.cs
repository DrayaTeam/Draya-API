namespace Draya.Domain.Wallets.Exceptions;

public class WithdrawalInsufficientBalanceException : Exception
{
    public WithdrawalInsufficientBalanceException(string message = "Insufficient available earned balance.") 
        : base(message)
    {
    }
}
