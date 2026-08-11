using Draya.Domain.Wallets;
using MediatR;

namespace Draya.Application.Wallets.Commands.DeletePayoutAccount;

public record DeletePayoutAccountCommand(Guid AccountId, Guid TeacherId) : IRequest<bool>;

public class DeletePayoutAccountCommandHandler : IRequestHandler<DeletePayoutAccountCommand, bool>
{
    private readonly ITeacherPayoutAccountRepository _payoutAccountRepository;

    public DeletePayoutAccountCommandHandler(ITeacherPayoutAccountRepository payoutAccountRepository)
    {
        _payoutAccountRepository = payoutAccountRepository;
    }

    public async Task<bool> Handle(DeletePayoutAccountCommand request, CancellationToken cancellationToken)
    {
        var account = await _payoutAccountRepository.GetByIdAsync(request.AccountId, cancellationToken);
        if (account == null || account.TeacherId != request.TeacherId)
        {
            throw new KeyNotFoundException("Payout account not found or access denied.");
        }

        await _payoutAccountRepository.DeleteAsync(account, cancellationToken);
        await _payoutAccountRepository.SaveChangesAsync(cancellationToken);
        return true;
    }
}
