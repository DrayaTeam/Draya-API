using Draya.Application.Payments.DTOs;
using MediatR;

namespace Draya.Application.Payments.Queries.GetPaymentStatus;

public record GetPaymentStatusQuery(Guid PaymentTransactionId, Guid UserId, string UserRole) : IRequest<PaymentStatusDto>;
