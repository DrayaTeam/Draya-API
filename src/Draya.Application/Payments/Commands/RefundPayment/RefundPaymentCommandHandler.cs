using Draya.Application.Payments.Services;
using MediatR;

namespace Draya.Application.Payments.Commands.RefundPayment;

public record RefundPaymentCommand(
    Guid PaymentTransactionId, 
    Guid AdminId
) : IRequest<bool>;

public class RefundPaymentCommandHandler : IRequestHandler<RefundPaymentCommand, bool>
{
    private readonly IPaymentRefundService _refundService;

    public RefundPaymentCommandHandler(IPaymentRefundService refundService)
    {
        _refundService = refundService;
    }

    public Task<bool> Handle(RefundPaymentCommand request, CancellationToken cancellationToken)
    {
        return _refundService.RefundPaymentAsync(request.PaymentTransactionId, request.AdminId, cancellationToken);
    }
}
