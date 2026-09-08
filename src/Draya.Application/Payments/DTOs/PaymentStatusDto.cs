namespace Draya.Application.Payments.DTOs;

public record PaymentStatusDto(
    Guid PaymentTransactionId,
    string Status,
    decimal GrossAmount,
    string Purpose,
    Guid? ClassroomId,
    bool IsEnrolled
);
