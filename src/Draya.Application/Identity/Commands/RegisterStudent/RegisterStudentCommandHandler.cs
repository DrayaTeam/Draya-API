using Draya.Application.Common.Interfaces;
using Draya.Application.Identity.DTOs;
using Draya.Domain.Identity;
using Draya.Domain.Identity.Exceptions;
using MediatR;

namespace Draya.Application.Identity.Commands.RegisterStudent;

public class RegisterStudentCommandHandler : IRequestHandler<RegisterStudentCommand, AuthResponseDto>
{
    private readonly IAppUserRepository _userRepository;
    private readonly IStudentRepository _studentRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;

    public RegisterStudentCommandHandler(
        IAppUserRepository userRepository,
        IStudentRepository studentRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IPasswordHasher passwordHasher,
        ITokenService tokenService)
    {
        _userRepository = userRepository;
        _studentRepository = studentRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
    }

    public async Task<AuthResponseDto> Handle(RegisterStudentCommand request, CancellationToken cancellationToken)
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
            Role = Role.Student,
            IsActive = true
        };

        var student = new Student
        {
            UserId = user.Id,
            FullName = request.FullName.Trim(),
            ParentGuardianEmail = request.ParentGuardianEmail.Trim().ToLowerInvariant(),
            DateOfBirth = request.DateOfBirth
        };

        await _userRepository.AddAsync(user, cancellationToken);
        await _studentRepository.AddAsync(student, cancellationToken);

        var (accessToken, expiresIn) = _tokenService.GenerateAccessToken(user, student.FullName);
        var refreshTokenValue = _tokenService.GenerateRefreshToken();

        var refreshToken = new Domain.Identity.RefreshToken
        {
            UserId = user.Id,
            Token = _tokenService.HashRefreshToken(refreshTokenValue),
            ExpiresAt = DateTime.UtcNow.AddDays(14)
        };

        await _refreshTokenRepository.AddAsync(refreshToken, cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);

        var userSummary = new UserSummaryDto(user.Id, student.FullName, user.Role.ToString());
        return new AuthResponseDto(accessToken, refreshTokenValue, expiresIn, userSummary);
    }
}
