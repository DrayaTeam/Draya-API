using Draya.Application.Payments.Services;
using MediatR;

namespace Draya.Application.Payments.Commands.ProcessPaymobWebhook;

public record ProcessPaymobWebhookCommand(
    Guid PaymentTransactionId,
    bool IsSuccess,
    string RawPayload
) : IRequest<bool>;

public class ProcessPaymobWebhookCommandHandler : IRequestHandler<ProcessPaymobWebhookCommand, bool>
{
    private readonly IPaymobWebhookProcessingService _webhookService;

    public ProcessPaymobWebhookCommandHandler(IPaymobWebhookProcessingService webhookService)
    {
        _webhookService = webhookService;
    }

    public Task<bool> Handle(ProcessPaymobWebhookCommand request, CancellationToken cancellationToken)
    {
        return _webhookService.ProcessWebhookAsync(
            request.PaymentTransactionId, 
            request.IsSuccess, 
            request.RawPayload, 
            cancellationToken);
    }
}
