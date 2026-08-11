namespace Draya.Application.Payments.Services;

public interface IPaymobService
{
    Task<string> CreateCheckoutUrlAsync(Guid paymentTransactionId, decimal amount, string email, string fullName, CancellationToken cancellationToken = default);
    bool VerifyHmac(IDictionary<string, string> queryOrBodyParams, string receivedHmac);
}
