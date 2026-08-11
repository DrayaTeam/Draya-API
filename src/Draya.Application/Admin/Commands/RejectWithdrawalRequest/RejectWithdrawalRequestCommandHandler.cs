using Draya.Domain.Wallets;
using MediatR;

namespace Draya.Application.Admin.Commands.RejectWithdrawalRequest;

public record RejectWithdrawalRequestCommand(
    Guid RequestId, 
    string RejectionReason, 
    Guid AdminId
) : IRequest<bool>;

public class RejectWithdrawalRequestCommandHandler : IRequestHandler<RejectWithdrawalRequestCommand, bool>
{
    private readonly IWithdrawalRequestRepository _withdrawalRepository;

    public RejectWithdrawalRequestCommandHandler(IWithdrawalRequestRepository withdrawalRepository)
    {
        _withdrawalRepository = withdrawalRepository;
    }

    public async Task<bool> Handle(RejectWithdrawalRequestCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RejectionReason))
        {
            throw new ArgumentException("Rejection reason is required.", nameof(request.RejectionReason));
        }

        var withdrawal = await _withdrawalRepository.GetByIdAsync(request.RequestId, cancellationToken);
        if (withdrawal == null)
        {
            throw new KeyNotFoundException("Withdrawal request not found.");
        }

        if (withdrawal.Status != WithdrawalStatus.Pending && withdrawal.Status != WithdrawalStatus.Approved)
        {
            throw new InvalidOperationException($"Cannot reject withdrawal request with status '{withdrawal.Status}'.");
        }

        withdrawal.Status = WithdrawalStatus.Rejected;
        withdrawal.RejectionReason = request.RejectionReason.Trim();
        withdrawal.ProcessedAt = DateTime.UtcNow;

        await _withdrawalRepository.UpdateAsync(withdrawal, cancellationToken);
        await _withdrawalRepository.SaveChangesAsync(cancellationToken);
        return true;
    }
}
