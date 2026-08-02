using Draya.Application.Common.Interfaces;
using Draya.Application.Identity.DTOs;
using Draya.Domain.Identity;
using Draya.Domain.Identity.Exceptions;
using MediatR;

namespace Draya.Application.Identity.Commands.RegisterTeacher;

public class RegisterTeacherCommandHandler : IRequestHandler<RegisterTeacherCommand, AuthResponseDto>
{
    private readonly IAppUserRepository _userRepository;
    private readonly ITeacherRepository _teacherRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;

    public RegisterTeacherCommandHandler(
        IAppUserRepository userRepository,
        ITeacherRepository teacherRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IPasswordHasher passwordHasher,
        ITokenService tokenService)
    {
        _userRepository = userRepository;
        _teacherRepository = teacherRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
    }

    public async Task<AuthResponseDto> Handle(RegisterTeacherCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        if (await _userRepository.ExistsByEmailAsync(normalizedEmail, cancellationToken))
        {
            throw new DuplicateEmailException(request.Email);
        }

        var passwordHash = _passwordHasher.HashPassword(request.Password);

        var user = new AppUser
        {
            Email = normalizedEmail,
            PasswordHash = passwordHash,
            Role = Role.Teacher,
            IsActive = true
        };

        var teacher = new Teacher
        {
            UserId = user.Id,
            FullName = request.FullName.Trim(),
            Phone = request.Phone?.Trim()
        };

        await _userRepository.AddAsync(user, cancellationToken);
        await _teacherRepository.AddAsync(teacher, cancellationToken);

        var (accessToken, expiresIn) = _tokenService.GenerateAccessToken(user, teacher.FullName);
        var refreshTokenValue = _tokenService.GenerateRefreshToken();

        var refreshToken = new Domain.Identity.RefreshToken
        {
            UserId = user.Id,
            Token = _tokenService.HashRefreshToken(refreshTokenValue),
            ExpiresAt = DateTime.UtcNow.AddDays(14)
        };

        await _refreshTokenRepository.AddAsync(refreshToken, cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);

        var userSummary = new UserSummaryDto(user.Id, teacher.FullName, user.Role.ToString());
        return new AuthResponseDto(accessToken, refreshTokenValue, expiresIn, userSummary);
    }
}
