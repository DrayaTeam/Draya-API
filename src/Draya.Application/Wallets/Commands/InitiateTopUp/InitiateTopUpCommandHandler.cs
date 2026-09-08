using Draya.Application.Payments.Services;
using Draya.Application.Wallets.DTOs;
using Draya.Domain.Payments;
using MediatR;

namespace Draya.Application.Wallets.Commands.InitiateTopUp;

public record InitiateTopUpCommand(
    Guid TeacherId, 
    decimal Amount,
    string RedirectionUrl
) : IRequest<TopUpCheckoutDto>;

public class InitiateTopUpCommandHandler : IRequestHandler<InitiateTopUpCommand, TopUpCheckoutDto>
{
    private readonly IPaymentTransactionRepository _paymentRepository;
    private readonly IPaymobService _paymobService;
    private readonly Draya.Domain.Identity.ITeacherRepository _teacherRepository;

    public InitiateTopUpCommandHandler(
        IPaymentTransactionRepository paymentRepository,
        IPaymobService paymobService,
        Draya.Domain.Identity.ITeacherRepository teacherRepository)
    {
        _paymentRepository = paymentRepository;
        _paymobService = paymobService;
        _teacherRepository = teacherRepository;
    }

    public async Task<TopUpCheckoutDto> Handle(InitiateTopUpCommand request, CancellationToken cancellationToken)
    {
        if (request.Amount <= 0)
        {
            throw new ArgumentException("Top-up amount must be greater than zero.", nameof(request.Amount));
        }

        if (string.IsNullOrWhiteSpace(request.RedirectionUrl))
            throw new ArgumentException("Redirection URL is required.");

        if (!Uri.TryCreate(request.RedirectionUrl, UriKind.Absolute, out var uri))
            throw new ArgumentException("Invalid Redirection URL format.");

        var teacher = await _teacherRepository.GetByUserIdAsync(request.TeacherId, cancellationToken);
        if (teacher == null)
            throw new UnauthorizedAccessException("Teacher not found.");

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
            RedirectionUrl = request.RedirectionUrl,
            CreatedAt = DateTime.UtcNow
        };

        await _paymentRepository.AddAsync(transaction, cancellationToken);
        await _paymentRepository.SaveChangesAsync(cancellationToken);

        string fullName = teacher.FullName ?? "Teacher";
        var nameParts = fullName.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        string firstName = nameParts.Length > 0 ? nameParts[0] : "Teacher";
        string lastName = nameParts.Length > 1 ? nameParts[1] : "Unknown";
        string phone = !string.IsNullOrEmpty(teacher.Phone) ? teacher.Phone : "01000000000";
        string email = "teacher@draya.com"; // Teacher entity does not have email

        var checkoutUrl = await _paymobService.CreateCheckoutUrlAsync(
            transaction.Id, 
            transaction.GrossAmount, 
            email, 
            firstName,
            lastName,
            phone,
            request.RedirectionUrl,
            cancellationToken);

        return new TopUpCheckoutDto(transaction.Id, transaction.GrossAmount, checkoutUrl);
    }
}
