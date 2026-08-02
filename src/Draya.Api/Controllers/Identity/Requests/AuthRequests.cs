namespace Draya.Api.Controllers.Identity.Requests;

public record RegisterTeacherRequest(
    string Email,
    string Password,
    string FullName,
    string? Phone
);

public record RegisterStudentRequest(
    string Email,
    string Password,
    string FullName,
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
