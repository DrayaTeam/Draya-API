namespace Draya.Domain.Wallets.Exceptions;

public class PayoutAccountNotOwnedException : Exception
{
    public PayoutAccountNotOwnedException(string message = "Payout account not found or does not belong to this teacher.") 
        : base(message)
    {
    }
}
