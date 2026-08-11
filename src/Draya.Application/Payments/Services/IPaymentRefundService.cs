namespace Draya.Application.Payments.Services;

public interface IPaymentRefundService
{
    Task<bool> RefundPaymentAsync(Guid paymentTransactionId, Guid adminId, CancellationToken cancellationToken = default);
}
