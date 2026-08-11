using Draya.Application.Identity.DTOs;

namespace Draya.Application.Common.Interfaces;

public interface IIdentityService
{
    Task<AuthResponseDto> RegisterTeacherAsync(string email, string password, string fullName, string? phone, CancellationToken cancellationToken);
    Task<AuthResponseDto> RegisterStudentAsync(string email, string password, string fullName, string parentGuardianEmail, DateTime? dateOfBirth, CancellationToken cancellationToken);
    Task<AuthResponseDto> LoginAsync(string email, string password, CancellationToken cancellationToken);
    Task<AuthResponseDto> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken);
    Task LogoutAsync(Guid userId, string? refreshToken, CancellationToken cancellationToken);
    Task<PasswordResetRequestResponseDto> RequestPasswordResetAsync(string email, CancellationToken cancellationToken);
    Task ConfirmPasswordResetAsync(string token, string newPassword, CancellationToken cancellationToken);
    Task<object> GetUserProfileAsync(Guid userId, CancellationToken cancellationToken);
}
