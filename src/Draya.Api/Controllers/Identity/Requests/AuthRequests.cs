namespace Draya.Api.Controllers.Identity.Requests;

public record RegisterTeacherRequest(
    string Email,
    string Password,
    string ConfirmPassword,
    string FullName,
    string? Phone,
    string? Specialization,
    string? Description
);

public record RegisterStudentRequest(
    string Email,
    string Password,
    string ConfirmPassword,
    string FullName,
    string ParentGuardianName,
    string ParentGuardianPhone,
    string ParentGuardianEmail,
    DateTime? DateOfBirth
);

public record LoginRequest(
    string Email,
    string Password
);

public record RefreshTokenRequest(
    string RefreshToken
);

public record LogoutRequest(string RefreshToken);

public record PasswordResetRequest(string Email);

public record PasswordResetConfirmationRequest(string Token, string NewPassword);

public record ChangePasswordRequest(string CurrentPassword, string NewPassword, string ConfirmPassword);

