using Draya.Application.Common.Interfaces;
using Draya.Application.Identity.DTOs;
using Draya.Domain.Identity;
using Draya.Domain.Identity.Exceptions;
using MediatR;

namespace Draya.Application.Identity.Commands.RefreshToken;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthResponseDto>
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IAppUserRepository _userRepository;
    private readonly ITeacherRepository _teacherRepository;
    private readonly IStudentRepository _studentRepository;
    private readonly ITokenService _tokenService;

    public RefreshTokenCommandHandler(
        IRefreshTokenRepository refreshTokenRepository,
        IAppUserRepository userRepository,
        ITeacherRepository teacherRepository,
        IStudentRepository studentRepository,
        ITokenService tokenService)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _userRepository = userRepository;
        _teacherRepository = teacherRepository;
        _studentRepository = studentRepository;
        _tokenService = tokenService;
    }

    public async Task<AuthResponseDto> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var storedToken = await _refreshTokenRepository.GetByTokenAsync(
            _tokenService.HashRefreshToken(request.Token), cancellationToken);

        if (storedToken is null || !storedToken.IsActive)
            throw new InvalidRefreshTokenException();

        var user = await _userRepository.GetByIdAsync(storedToken.UserId, cancellationToken);
        if (user is null || !user.IsActive)
            throw new AccountDeactivatedException();

        // Rotate: revoke old, issue new
        storedToken.RevokedAt = DateTime.UtcNow;
        await _refreshTokenRepository.UpdateAsync(storedToken, cancellationToken);

        var fullName = user.Role switch
        {
            Role.Teacher => (await _teacherRepository.GetByUserIdAsync(user.Id, cancellationToken))?.FullName ?? string.Empty,
            Role.Student => (await _studentRepository.GetByUserIdAsync(user.Id, cancellationToken))?.FullName ?? string.Empty,
            _ => string.Empty
        };

        var (accessToken, expiresIn) = _tokenService.GenerateAccessToken(user, fullName);
        var newRefreshTokenValue = _tokenService.GenerateRefreshToken();

        var newRefreshToken = new Domain.Identity.RefreshToken
        {
            UserId = user.Id,
            Token = _tokenService.HashRefreshToken(newRefreshTokenValue),
            ExpiresAt = DateTime.UtcNow.AddDays(14)
        };

        await _refreshTokenRepository.AddAsync(newRefreshToken, cancellationToken);
        await _refreshTokenRepository.SaveChangesAsync(cancellationToken);

        return new AuthResponseDto(accessToken, newRefreshTokenValue, expiresIn, new UserSummaryDto(user.Id, fullName, user.Role.ToString()));
    }
}
