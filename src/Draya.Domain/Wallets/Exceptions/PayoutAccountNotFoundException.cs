namespace Draya.Domain.Wallets.Exceptions;

public class PayoutAccountNotFoundException : Exception
{
    public PayoutAccountNotFoundException(string message = "Payout account not found.") 
        : base(message)
    {
    }
}
