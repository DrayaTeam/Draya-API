using Draya.Application.Payments.Services;
using Draya.Application.Wallets.DTOs;
using Draya.Domain.Payments;
using MediatR;

namespace Draya.Application.Wallets.Commands.InitiateTopUp;

public record InitiateTopUpCommand(
    Guid TeacherId, 
    decimal Amount
) : IRequest<TopUpCheckoutDto>;

public class InitiateTopUpCommandHandler : IRequestHandler<InitiateTopUpCommand, TopUpCheckoutDto>
{
    private readonly IPaymentTransactionRepository _paymentRepository;
    private readonly IPaymobService _paymobService;

    public InitiateTopUpCommandHandler(
        IPaymentTransactionRepository paymentRepository,
        IPaymobService paymobService)
    {
        _paymentRepository = paymentRepository;
        _paymobService = paymobService;
    }

    public async Task<TopUpCheckoutDto> Handle(InitiateTopUpCommand request, CancellationToken cancellationToken)
    {
        if (request.Amount <= 0)
        {
            throw new ArgumentException("Top-up amount must be greater than zero.", nameof(request.Amount));
        }

        var transaction = new PaymentTransaction
        {
            Id = Guid.NewGuid(),
            Purpose = PaymentPurpose.TeacherTopUp,
            PayerId = request.TeacherId,
            ClassroomId = null,
            GrossAmount = request.Amount,
            CommissionPercent = null,
            CommissionAmount = null,
            TeacherAmount = null,
            Status = PaymentStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        await _paymentRepository.AddAsync(transaction, cancellationToken);
        await _paymentRepository.SaveChangesAsync(cancellationToken);

        var checkoutUrl = await _paymobService.CreateCheckoutUrlAsync(
            transaction.Id, 
            transaction.GrossAmount, 
            "teacher@draya.com", 
            "Teacher", 
            cancellationToken);

        return new TopUpCheckoutDto(transaction.Id, transaction.GrossAmount, checkoutUrl);
    }
}
