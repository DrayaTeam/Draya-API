namespace Draya.Application.Payments.Services;

public interface IPaymobWebhookProcessingService
{
    Task<bool> ProcessWebhookAsync(Guid paymentTransactionId, bool isSuccess, string rawPayload, CancellationToken cancellationToken = default);
}
