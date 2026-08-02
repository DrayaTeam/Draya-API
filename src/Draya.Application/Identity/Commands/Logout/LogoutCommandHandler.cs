using Draya.Application.Common.Interfaces;
using Draya.Domain.Identity;
using Draya.Domain.Identity.Exceptions;
using MediatR;

namespace Draya.Application.Identity.Commands.Logout;

public class LogoutCommandHandler : IRequestHandler<LogoutCommand>
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ITokenService _tokenService;

    public LogoutCommandHandler(IRefreshTokenRepository refreshTokenRepository, ITokenService tokenService)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _tokenService = tokenService;
    }

    public async Task Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            await _refreshTokenRepository.RevokeAllForUserAsync(request.UserId, cancellationToken);
            await _refreshTokenRepository.SaveChangesAsync(cancellationToken);
            return;
        }

        var token = await _refreshTokenRepository.GetByTokenAsync(
            _tokenService.HashRefreshToken(request.RefreshToken), cancellationToken);

        if (token is null || token.UserId != request.UserId || !token.IsActive)
            throw new InvalidRefreshTokenException();

        token.RevokedAt = DateTime.UtcNow;
        await _refreshTokenRepository.UpdateAsync(token, cancellationToken);
        await _refreshTokenRepository.SaveChangesAsync(cancellationToken);
    }
}
