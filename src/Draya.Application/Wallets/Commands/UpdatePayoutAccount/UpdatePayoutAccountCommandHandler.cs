using Draya.Application.Wallets.DTOs;
using Draya.Domain.Wallets;
using MediatR;

namespace Draya.Application.Wallets.Commands.UpdatePayoutAccount;

public record UpdatePayoutAccountCommand(
    Guid AccountId,
    Guid TeacherId,
    PayoutAccountType AccountType,
    string AccountName,
    string AccountIdentifier,
    bool IsDefault
) : IRequest<TeacherPayoutAccountDto>;

public class UpdatePayoutAccountCommandHandler : IRequestHandler<UpdatePayoutAccountCommand, TeacherPayoutAccountDto>
{
    private readonly ITeacherPayoutAccountRepository _payoutAccountRepository;

    public UpdatePayoutAccountCommandHandler(ITeacherPayoutAccountRepository payoutAccountRepository)
    {
        _payoutAccountRepository = payoutAccountRepository;
    }

    public async Task<TeacherPayoutAccountDto> Handle(UpdatePayoutAccountCommand request, CancellationToken cancellationToken)
    {
        var account = await _payoutAccountRepository.GetByIdAsync(request.AccountId, cancellationToken);
        if (account == null || account.TeacherId != request.TeacherId)
        {
            throw new KeyNotFoundException("Payout account not found or access denied.");
        }

        if (request.IsDefault && !account.IsDefault)
        {
            var existingDefault = await _payoutAccountRepository.GetDefaultByTeacherIdAsync(request.TeacherId, cancellationToken);
            if (existingDefault != null)
            {
                existingDefault.IsDefault = false;
                await _payoutAccountRepository.UpdateAsync(existingDefault, cancellationToken);
            }
        }

        account.AccountType = request.AccountType;
        account.AccountName = request.AccountName.Trim();
        account.AccountIdentifier = request.AccountIdentifier.Trim();
        account.IsDefault = request.IsDefault;
        account.UpdatedAt = DateTime.UtcNow;

        await _payoutAccountRepository.UpdateAsync(account, cancellationToken);
        await _payoutAccountRepository.SaveChangesAsync(cancellationToken);

        return new TeacherPayoutAccountDto(
            account.Id,
            account.TeacherId,
            account.AccountType,
            account.AccountName,
            account.AccountIdentifier,
            account.IsDefault,
            account.CreatedAt
        );
    }
}
