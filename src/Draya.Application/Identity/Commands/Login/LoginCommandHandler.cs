using Draya.Application.Common.Interfaces;
using Draya.Application.Identity.DTOs;
using Draya.Domain.Identity;
using Draya.Domain.Identity.Exceptions;
using MediatR;

namespace Draya.Application.Identity.Commands.Login;

public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResponseDto>
{
    private readonly IAppUserRepository _userRepository;
    private readonly ITeacherRepository _teacherRepository;
    private readonly IStudentRepository _studentRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IAccountSecuritySettings _accountSecuritySettings;

    public LoginCommandHandler(
        IAppUserRepository userRepository,
        ITeacherRepository teacherRepository,
        IStudentRepository studentRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IAccountSecuritySettings accountSecuritySettings)
    {
        _userRepository = userRepository;
        _teacherRepository = teacherRepository;
        _studentRepository = studentRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _accountSecuritySettings = accountSecuritySettings;
    }

    public async Task<AuthResponseDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await _userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);

        if (user is null)
            throw new InvalidCredentialsException();

        var now = DateTime.UtcNow;
        if (user.LockoutEndUtc > now)
            throw new AccountLockedException();

        if (user.LockoutEndUtc is not null)
        {
            user.LockoutEndUtc = null;
            user.FailedLoginAttempts = 0;
        }

        if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            user.FailedLoginAttempts++;
            if (user.FailedLoginAttempts >= _accountSecuritySettings.LockoutThreshold)
            {
                user.LockoutEndUtc = now.AddMinutes(_accountSecuritySettings.LockoutCooldownMinutes);
                await _userRepository.UpdateAsync(user, cancellationToken);
                await _userRepository.SaveChangesAsync(cancellationToken);
                throw new AccountLockedException();
            }

            await _userRepository.UpdateAsync(user, cancellationToken);
            await _userRepository.SaveChangesAsync(cancellationToken);
            throw new InvalidCredentialsException();
        }

        if (!user.IsActive)
            throw new AccountDeactivatedException();

        // Resolve display name from role-specific profile
        var fullName = user.Role switch
        {
            Role.Teacher => (await _teacherRepository.GetByUserIdAsync(user.Id, cancellationToken))?.FullName ?? string.Empty,
            Role.Student => (await _studentRepository.GetByUserIdAsync(user.Id, cancellationToken))?.FullName ?? string.Empty,
            _ => string.Empty
        };

        user.LastLoginAt = DateTime.UtcNow;
        user.FailedLoginAttempts = 0;
        user.LockoutEndUtc = null;
        await _userRepository.UpdateAsync(user, cancellationToken);

        var (accessToken, expiresIn) = _tokenService.GenerateAccessToken(user, fullName);
        var refreshTokenValue = _tokenService.GenerateRefreshToken();

        var refreshToken = new Domain.Identity.RefreshToken
        {
            UserId = user.Id,
            Token = _tokenService.HashRefreshToken(refreshTokenValue),
            ExpiresAt = DateTime.UtcNow.AddDays(14)
        };

        await _refreshTokenRepository.AddAsync(refreshToken, cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);

        return new AuthResponseDto(accessToken, refreshTokenValue, expiresIn, new UserSummaryDto(user.Id, fullName, user.Role.ToString()));
    }
}
