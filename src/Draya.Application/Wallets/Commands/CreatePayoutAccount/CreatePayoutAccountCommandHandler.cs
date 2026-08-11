using Draya.Application.Wallets.DTOs;
using Draya.Domain.Wallets;
using MediatR;

namespace Draya.Application.Wallets.Commands.CreatePayoutAccount;

public record CreatePayoutAccountCommand(
    Guid TeacherId,
    PayoutAccountType AccountType,
    string AccountName,
    string AccountIdentifier,
    bool IsDefault = false
) : IRequest<TeacherPayoutAccountDto>;

public class CreatePayoutAccountCommandHandler : IRequestHandler<CreatePayoutAccountCommand, TeacherPayoutAccountDto>
{
    private readonly ITeacherPayoutAccountRepository _payoutAccountRepository;

    public CreatePayoutAccountCommandHandler(ITeacherPayoutAccountRepository payoutAccountRepository)
    {
        _payoutAccountRepository = payoutAccountRepository;
    }

    public async Task<TeacherPayoutAccountDto> Handle(CreatePayoutAccountCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.AccountName))
        {
            throw new ArgumentException("Account name is required.", nameof(request.AccountName));
        }

        if (string.IsNullOrWhiteSpace(request.AccountIdentifier))
        {
            throw new ArgumentException("Account identifier (IBAN/wallet phone) is required.", nameof(request.AccountIdentifier));
        }

        if (request.IsDefault)
        {
            var existingDefault = await _payoutAccountRepository.GetDefaultByTeacherIdAsync(request.TeacherId, cancellationToken);
            if (existingDefault != null)
            {
                existingDefault.IsDefault = false;
                await _payoutAccountRepository.UpdateAsync(existingDefault, cancellationToken);
            }
        }

        var account = new TeacherPayoutAccount
        {
            Id = Guid.NewGuid(),
            TeacherId = request.TeacherId,
            AccountType = request.AccountType,
            AccountName = request.AccountName.Trim(),
            AccountIdentifier = request.AccountIdentifier.Trim(),
            IsDefault = request.IsDefault,
            CreatedAt = DateTime.UtcNow
        };

        await _payoutAccountRepository.AddAsync(account, cancellationToken);
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
