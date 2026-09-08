using Draya.Application.Payments.DTOs;
using Draya.Domain.Classrooms;
using Draya.Domain.Payments;
using MediatR;

namespace Draya.Application.Payments.Queries.GetPaymentStatus;

public class GetPaymentStatusQueryHandler : IRequestHandler<GetPaymentStatusQuery, PaymentStatusDto>
{
    private readonly IPaymentTransactionRepository _paymentTransactionRepository;
    private readonly IEnrollmentRepository _enrollmentRepository;

    public GetPaymentStatusQueryHandler(
        IPaymentTransactionRepository paymentTransactionRepository,
        IEnrollmentRepository enrollmentRepository)
    {
        _paymentTransactionRepository = paymentTransactionRepository;
        _enrollmentRepository = enrollmentRepository;
    }

    public async Task<PaymentStatusDto> Handle(GetPaymentStatusQuery request, CancellationToken cancellationToken)
    {
        var transaction = await _paymentTransactionRepository.GetByIdAsync(request.PaymentTransactionId, cancellationToken);
        
        if (transaction == null)
            throw new KeyNotFoundException("Payment transaction not found.");

        if (transaction.PayerId != request.UserId)
            throw new UnauthorizedAccessException("You are not authorized to view this transaction.");

        bool isEnrolled = false;
        
        if (transaction.Purpose == PaymentPurpose.ClassroomEnrollment && transaction.ClassroomId.HasValue && request.UserRole == "Student")
        {
            var enrollment = await _enrollmentRepository.GetByStudentAndClassroomAsync(request.UserId, transaction.ClassroomId.Value, cancellationToken);
            isEnrolled = enrollment != null && enrollment.Status == EnrollmentStatus.Active;
        }

        return new PaymentStatusDto(
            transaction.Id,
            transaction.Status.ToString(),
            transaction.GrossAmount,
            transaction.Purpose.ToString(),
            transaction.ClassroomId,
            isEnrolled
        );
    }
}
