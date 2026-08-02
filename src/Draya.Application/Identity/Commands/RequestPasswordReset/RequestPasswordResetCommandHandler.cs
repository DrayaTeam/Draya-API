using Draya.Application.Common.Interfaces;
using Draya.Application.Identity.DTOs;
using Draya.Domain.Identity;
using MediatR;

namespace Draya.Application.Identity.Commands.RequestPasswordReset;

public class RequestPasswordResetCommandHandler : IRequestHandler<RequestPasswordResetCommand, PasswordResetRequestResponseDto>
{
    private const string ResponseMessage = "If the account exists, a password reset email has been sent.";
    private readonly IAppUserRepository _userRepository;
    private readonly IPasswordResetTokenRepository _passwordResetTokenRepository;
    private readonly ITokenService _tokenService;
    private readonly IEmailService _emailService;
    private readonly IAccountSecuritySettings _accountSecuritySettings;

    public RequestPasswordResetCommandHandler(IAppUserRepository userRepository, IPasswordResetTokenRepository passwordResetTokenRepository,
        ITokenService tokenService, IEmailService emailService, IAccountSecuritySettings accountSecuritySettings)
    {
        _userRepository = userRepository;
        _passwordResetTokenRepository = passwordResetTokenRepository;
        _tokenService = tokenService;
        _emailService = emailService;
        _accountSecuritySettings = accountSecuritySettings;
    }

    public async Task<PasswordResetRequestResponseDto> Handle(RequestPasswordResetCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email.Trim().ToLowerInvariant(), cancellationToken);
        if (user is null)
            return new PasswordResetRequestResponseDto(ResponseMessage);

        var tokenValue = _tokenService.GenerateRefreshToken();
        var resetToken = new PasswordResetToken
        {
            UserId = user.Id,
            TokenHash = _tokenService.HashRefreshToken(tokenValue),
            ExpiresAt = DateTime.UtcNow.AddMinutes(_accountSecuritySettings.PasswordResetTokenExpiryMinutes)
        };

        await _passwordResetTokenRepository.AddAsync(resetToken, cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);
        try
        {
            await _emailService.SendPasswordResetEmailAsync(user.Email, tokenValue, cancellationToken);
        }
        catch
        {
            // Keep the externally observable response identical for all email addresses.
        }

        return new PasswordResetRequestResponseDto(ResponseMessage);
    }
}
