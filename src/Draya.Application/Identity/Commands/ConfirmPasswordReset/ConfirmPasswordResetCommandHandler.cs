using Draya.Application.Common.Interfaces;
using Draya.Domain.Identity;
using Draya.Domain.Identity.Exceptions;
using MediatR;

namespace Draya.Application.Identity.Commands.ConfirmPasswordReset;

public class ConfirmPasswordResetCommandHandler : IRequestHandler<ConfirmPasswordResetCommand>
{
    private readonly IPasswordResetTokenRepository _passwordResetTokenRepository;
    private readonly IAppUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;

    public ConfirmPasswordResetCommandHandler(IPasswordResetTokenRepository passwordResetTokenRepository, IAppUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository, IPasswordHasher passwordHasher, ITokenService tokenService)
    {
        _passwordResetTokenRepository = passwordResetTokenRepository;
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
    }

    public async Task Handle(ConfirmPasswordResetCommand request, CancellationToken cancellationToken)
    {
        var resetToken = await _passwordResetTokenRepository.GetByTokenHashAsync(
            _tokenService.HashRefreshToken(request.Token), cancellationToken);
        if (resetToken is null || !resetToken.IsActive)
            throw new InvalidPasswordResetTokenException();

        var user = await _userRepository.GetByIdAsync(resetToken.UserId, cancellationToken)
            ?? throw new InvalidPasswordResetTokenException();

        user.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
        user.FailedLoginAttempts = 0;
        user.LockoutEndUtc = null;
        resetToken.UsedAt = DateTime.UtcNow;

        await _userRepository.UpdateAsync(user, cancellationToken);
        await _passwordResetTokenRepository.UpdateAsync(resetToken, cancellationToken);
        await _refreshTokenRepository.RevokeAllForUserAsync(user.Id, cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);
    }
}
