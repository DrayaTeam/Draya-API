using Draya.Application.Common.Interfaces;
using Draya.Application.Identity.DTOs;
using Draya.Domain.Identity;
using Draya.Domain.Identity.Exceptions;
using Draya.Domain.Wallets;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Draya.Infrastructure.Identity.Services;

public class IdentityService : IIdentityService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly ITeacherRepository _teacherRepository;
    private readonly IStudentRepository _studentRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ITeacherWalletRepository _teacherWalletRepository;
    private readonly ITokenService _tokenService;
    private readonly IEmailService _emailService;

    public IdentityService(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        ITeacherRepository teacherRepository,
        IStudentRepository studentRepository,
        IRefreshTokenRepository refreshTokenRepository,
        ITeacherWalletRepository teacherWalletRepository,
        ITokenService tokenService,
        IEmailService emailService)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _teacherRepository = teacherRepository;
        _studentRepository = studentRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _teacherWalletRepository = teacherWalletRepository;
        _tokenService = tokenService;
        _emailService = emailService;
    }

    public async Task<AuthResponseDto> RegisterTeacherAsync(
        string email,
        string password,
        string fullName,
        string? phone,
        string? specialization,
        string? description,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var existingUser = await _userManager.FindByEmailAsync(normalizedEmail);
        if (existingUser is not null)
        {
            throw new DuplicateEmailException(email);
        }

        await EnsureRoleExistsAsync(Role.Teacher.ToString());

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = normalizedEmail,
            Email = normalizedEmail,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"User registration failed: {errors}");
        }

        await _userManager.AddToRoleAsync(user, Role.Teacher.ToString());

        var teacher = new Teacher
        {
            UserId = user.Id,
            FullName = fullName.Trim(),
            Phone = phone?.Trim(),
            Specialization = specialization?.Trim(),
            Description = description?.Trim()
        };
        await _teacherRepository.AddAsync(teacher, cancellationToken);

        var teacherWallet = new TeacherWallet
        {
            TeacherId = teacher.UserId,
            EarnedBalance = 0m,
            PurchasedBalance = 0m
        };
        await _teacherWalletRepository.AddAsync(teacherWallet, cancellationToken);

        var (accessToken, expiresIn) = _tokenService.GenerateAccessToken(user.Id, user.Email!, Role.Teacher.ToString(), teacher.FullName);
        var refreshTokenValue = _tokenService.GenerateRefreshToken();

        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            Token = _tokenService.HashRefreshToken(refreshTokenValue),
            ExpiresAt = DateTime.UtcNow.AddDays(14)
        };

        await _refreshTokenRepository.AddAsync(refreshToken, cancellationToken);
        await _teacherRepository.SaveChangesAsync(cancellationToken);

        var userSummary = new UserSummaryDto(user.Id, teacher.FullName, Role.Teacher.ToString());
        return new AuthResponseDto(accessToken, refreshTokenValue, expiresIn, userSummary);
    }

    public async Task<AuthResponseDto> RegisterStudentAsync(
        string email,
        string password,
        string fullName,
        string parentGuardianEmail,
        DateTime? dateOfBirth,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var existingUser = await _userManager.FindByEmailAsync(normalizedEmail);
        if (existingUser is not null)
        {
            throw new DuplicateEmailException(email);
        }

        await EnsureRoleExistsAsync(Role.Student.ToString());

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = normalizedEmail,
            Email = normalizedEmail,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"User registration failed: {errors}");
        }

        await _userManager.AddToRoleAsync(user, Role.Student.ToString());

        var student = new Student
        {
            UserId = user.Id,
            FullName = fullName.Trim(),
            ParentGuardianEmail = parentGuardianEmail.Trim().ToLowerInvariant(),
            DateOfBirth = dateOfBirth
        };
        await _studentRepository.AddAsync(student, cancellationToken);

        var (accessToken, expiresIn) = _tokenService.GenerateAccessToken(user.Id, user.Email!, Role.Student.ToString(), student.FullName);
        var refreshTokenValue = _tokenService.GenerateRefreshToken();

        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            Token = _tokenService.HashRefreshToken(refreshTokenValue),
            ExpiresAt = DateTime.UtcNow.AddDays(14)
        };

        await _refreshTokenRepository.AddAsync(refreshToken, cancellationToken);
        await _studentRepository.SaveChangesAsync(cancellationToken);

        var userSummary = new UserSummaryDto(user.Id, student.FullName, Role.Student.ToString());
        return new AuthResponseDto(accessToken, refreshTokenValue, expiresIn, userSummary);
    }

    public async Task<AuthResponseDto> LoginAsync(string email, string password, CancellationToken cancellationToken)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var user = await _userManager.FindByEmailAsync(normalizedEmail);
        if (user is null)
        {
            throw new InvalidCredentialsException();
        }

        if (!user.IsActive)
        {
            throw new AccountDeactivatedException();
        }

        if (await _userManager.IsLockedOutAsync(user))
        {
            throw new AccountLockedException();
        }

        var isPasswordValid = await _userManager.CheckPasswordAsync(user, password);
        if (!isPasswordValid)
        {
            await _userManager.AccessFailedAsync(user);
            if (await _userManager.IsLockedOutAsync(user))
            {
                throw new AccountLockedException();
            }
            throw new InvalidCredentialsException();
        }

        await _userManager.ResetAccessFailedCountAsync(user);
        user.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        var roles = await _userManager.GetRolesAsync(user);
        var primaryRole = roles.FirstOrDefault() ?? Role.Student.ToString();

        var fullName = primaryRole switch
        {
            nameof(Role.Teacher) => (await _teacherRepository.GetByUserIdAsync(user.Id, cancellationToken))?.FullName ?? string.Empty,
            nameof(Role.Student) => (await _studentRepository.GetByUserIdAsync(user.Id, cancellationToken))?.FullName ?? string.Empty,
            _ => string.Empty
        };

        var (accessToken, expiresIn) = _tokenService.GenerateAccessToken(user.Id, user.Email!, primaryRole, fullName);
        var refreshTokenValue = _tokenService.GenerateRefreshToken();

        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            Token = _tokenService.HashRefreshToken(refreshTokenValue),
            ExpiresAt = DateTime.UtcNow.AddDays(14)
        };

        await _refreshTokenRepository.AddAsync(refreshToken, cancellationToken);
        await _refreshTokenRepository.SaveChangesAsync(cancellationToken);

        return new AuthResponseDto(accessToken, refreshTokenValue, expiresIn, new UserSummaryDto(user.Id, fullName, primaryRole));
    }

    public async Task<AuthResponseDto> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var hashedToken = _tokenService.HashRefreshToken(refreshToken);
        var storedToken = await _refreshTokenRepository.GetByTokenAsync(hashedToken, cancellationToken);
        if (storedToken is null || !storedToken.IsActive)
        {
            throw new InvalidRefreshTokenException();
        }

        var user = await _userManager.FindByIdAsync(storedToken.UserId.ToString());
        if (user is null || !user.IsActive)
        {
            throw new AccountDeactivatedException();
        }

        storedToken.RevokedAt = DateTime.UtcNow;
        await _refreshTokenRepository.UpdateAsync(storedToken, cancellationToken);

        var roles = await _userManager.GetRolesAsync(user);
        var primaryRole = roles.FirstOrDefault() ?? Role.Student.ToString();

        var fullName = primaryRole switch
        {
            nameof(Role.Teacher) => (await _teacherRepository.GetByUserIdAsync(user.Id, cancellationToken))?.FullName ?? string.Empty,
            nameof(Role.Student) => (await _studentRepository.GetByUserIdAsync(user.Id, cancellationToken))?.FullName ?? string.Empty,
            _ => string.Empty
        };

        var (accessToken, expiresIn) = _tokenService.GenerateAccessToken(user.Id, user.Email!, primaryRole, fullName);
        var newRefreshTokenValue = _tokenService.GenerateRefreshToken();

        var newRefreshToken = new RefreshToken
        {
            UserId = user.Id,
            Token = _tokenService.HashRefreshToken(newRefreshTokenValue),
            ExpiresAt = DateTime.UtcNow.AddDays(14)
        };

        await _refreshTokenRepository.AddAsync(newRefreshToken, cancellationToken);
        await _refreshTokenRepository.SaveChangesAsync(cancellationToken);

        return new AuthResponseDto(accessToken, newRefreshTokenValue, expiresIn, new UserSummaryDto(user.Id, fullName, primaryRole));
    }

    public async Task LogoutAsync(Guid userId, string? refreshToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            await _refreshTokenRepository.RevokeAllForUserAsync(userId, cancellationToken);
            await _refreshTokenRepository.SaveChangesAsync(cancellationToken);
            return;
        }

        var hashedToken = _tokenService.HashRefreshToken(refreshToken);
        var token = await _refreshTokenRepository.GetByTokenAsync(hashedToken, cancellationToken);
        if (token is null || token.UserId != userId || !token.IsActive)
        {
            throw new InvalidRefreshTokenException();
        }

        token.RevokedAt = DateTime.UtcNow;
        await _refreshTokenRepository.UpdateAsync(token, cancellationToken);
        await _refreshTokenRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task<PasswordResetRequestResponseDto> RequestPasswordResetAsync(string email, CancellationToken cancellationToken)
    {
        const string responseMessage = "If the account exists, a password reset email has been sent.";
        var user = await _userManager.FindByEmailAsync(email.Trim().ToLowerInvariant());
        if (user is null)
        {
            return new PasswordResetRequestResponseDto(responseMessage);
        }

        var tokenValue = await _userManager.GeneratePasswordResetTokenAsync(user);
        try
        {
            await _emailService.SendPasswordResetEmailAsync(user.Email!, tokenValue, cancellationToken);
        }
        catch
        {
            // Preserve response for security
        }

        return new PasswordResetRequestResponseDto(responseMessage);
    }

    public async Task ConfirmPasswordResetAsync(string token, string newPassword, CancellationToken cancellationToken)
    {
        var users = await _userManager.Users.ToListAsync(cancellationToken);
        ApplicationUser? targetUser = null;

        foreach (var user in users)
        {
            var isValid = await _userManager.VerifyUserTokenAsync(
                user,
                _userManager.Options.Tokens.PasswordResetTokenProvider,
                UserManager<ApplicationUser>.ResetPasswordTokenPurpose,
                token);

            if (isValid)
            {
                targetUser = user;
                break;
            }
        }

        if (targetUser is null)
        {
            throw new InvalidPasswordResetTokenException();
        }

        var result = await _userManager.ResetPasswordAsync(targetUser, token, newPassword);
        if (!result.Succeeded)
        {
            throw new InvalidPasswordResetTokenException();
        }

        await _userManager.ResetAccessFailedCountAsync(targetUser);
        await _refreshTokenRepository.RevokeAllForUserAsync(targetUser.Id, cancellationToken);
        await _refreshTokenRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            throw new UnauthorizedAccessException("User not found.");
        }

        var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        if (!result.Succeeded)
        {
            var isPasswordMismatch = result.Errors.Any(e => e.Code == "PasswordMismatch");
            if (isPasswordMismatch)
            {
                throw new InvalidCurrentPasswordException();
            }

            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new InvalidCurrentPasswordException(errors);
        }

        await _userManager.UpdateSecurityStampAsync(user);
    }

    public async Task<object> GetUserProfileAsync(Guid userId, CancellationToken cancellationToken)

    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            throw new UnauthorizedAccessException("User not found.");
        }

        var roles = await _userManager.GetRolesAsync(user);
        var primaryRole = roles.FirstOrDefault() ?? Role.Student.ToString();

        if (primaryRole == nameof(Role.Teacher))
        {
            var teacher = await _teacherRepository.GetByUserIdAsync(user.Id, cancellationToken)
                ?? throw new UnauthorizedAccessException("Teacher profile not found.");
            return new TeacherProfileDto(
                user.Id,
                user.Email!,
                teacher.FullName,
                teacher.Phone,
                teacher.Specialization,
                teacher.Description,
                teacher.ProfilePictureUrl);
        }

        if (primaryRole == nameof(Role.Student))
        {
            var student = await _studentRepository.GetByUserIdAsync(user.Id, cancellationToken)
                ?? throw new UnauthorizedAccessException("Student profile not found.");
            return new StudentProfileDto(
                user.Id,
                user.Email!,
                student.FullName,
                student.ParentGuardianEmail,
                student.DateOfBirth,
                student.ProfilePictureUrl);
        }

        return new UserSummaryDto(user.Id, "Admin", primaryRole);
    }


    private async Task EnsureRoleExistsAsync(string roleName)
    {
        if (!await _roleManager.RoleExistsAsync(roleName))
        {
            await _roleManager.CreateAsync(new IdentityRole<Guid>
            {
                Id = Guid.NewGuid(),
                Name = roleName,
                NormalizedName = roleName.ToUpperInvariant()
            });
        }
    }
}
