using Draya.Application.Identity.DTOs;

namespace Draya.Application.Common.Interfaces;

public interface IIdentityService
{
    Task<AuthResponseDto> RegisterTeacherAsync(string email, string password, string fullName, string? phone, string? specialization, string? description, CancellationToken cancellationToken);
    Task<AuthResponseDto> RegisterStudentAsync(string email, string password, string fullName, string parentGuardianEmail, string parentGuardianName, string parentGuardianPhone, DateTime? dateOfBirth, CancellationToken cancellationToken);
    Task<AuthResponseDto> LoginAsync(string email, string password, CancellationToken cancellationToken);
    Task<AuthResponseDto> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken);
    Task LogoutAsync(Guid userId, string? refreshToken, CancellationToken cancellationToken);
    Task<PasswordResetRequestResponseDto> RequestPasswordResetAsync(string email, CancellationToken cancellationToken);
    Task ConfirmPasswordResetAsync(string token, string newPassword, CancellationToken cancellationToken);
    Task ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken cancellationToken);
    Task<object> GetUserProfileAsync(Guid userId, CancellationToken cancellationToken);
    
    // Admin & Supervisor operations
    Task UpdateAdminProfileAsync(Guid userId, string fullName, string email, string? phoneNumber, CancellationToken cancellationToken);
    Task<SupervisorDto> InviteSupervisorAsync(string name, string email, string role, CancellationToken cancellationToken);
    Task ResendSupervisorInviteAsync(Guid supervisorId, CancellationToken cancellationToken);
    Task AcceptSupervisorInviteAsync(string email, string token, string newPassword, CancellationToken cancellationToken);
    Task<List<SupervisorDto>> GetSupervisorsAsync(CancellationToken cancellationToken);
    Task ToggleSupervisorStatusAsync(Guid supervisorId, bool isActive, CancellationToken cancellationToken);
    Task<List<TeacherSearchDto>> SearchTeachersForAdminAsync(string? query, CancellationToken cancellationToken);
    Task<(List<AdminStudentDto> Items, int TotalCount)> SearchStudentsForAdminAsync(string? query, int page, int pageSize, CancellationToken cancellationToken);
}

