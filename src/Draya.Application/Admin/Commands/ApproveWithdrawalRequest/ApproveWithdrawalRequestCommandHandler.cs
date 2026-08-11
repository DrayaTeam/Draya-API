using Draya.Application.Admin.DTOs;
using Draya.Domain.Wallets;
using MediatR;

namespace Draya.Application.Admin.Commands.ApproveWithdrawalRequest;

public record ApproveWithdrawalRequestCommand(Guid RequestId, Guid AdminId) : IRequest<bool>;

public class ApproveWithdrawalRequestCommandHandler : IRequestHandler<ApproveWithdrawalRequestCommand, bool>
{
    private readonly IWithdrawalRequestRepository _withdrawalRepository;

    public ApproveWithdrawalRequestCommandHandler(IWithdrawalRequestRepository withdrawalRepository)
    {
        _withdrawalRepository = withdrawalRepository;
    }

    public async Task<bool> Handle(ApproveWithdrawalRequestCommand request, CancellationToken cancellationToken)
    {
        var withdrawal = await _withdrawalRepository.GetByIdAsync(request.RequestId, cancellationToken);
        if (withdrawal == null)
        {
            throw new KeyNotFoundException("Withdrawal request not found.");
        }

        if (withdrawal.Status != WithdrawalStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot approve withdrawal request with status '{withdrawal.Status}'.");
        }

        withdrawal.Status = WithdrawalStatus.Approved;
        withdrawal.ProcessedAt = DateTime.UtcNow;

        await _withdrawalRepository.UpdateAsync(withdrawal, cancellationToken);
        await _withdrawalRepository.SaveChangesAsync(cancellationToken);
        return true;
    }
}
