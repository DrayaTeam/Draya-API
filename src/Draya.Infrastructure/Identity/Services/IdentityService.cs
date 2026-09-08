using Draya.Application.Common.Interfaces;
using Draya.Application.Identity.DTOs;
using Draya.Domain.Identity;
using Draya.Domain.Identity.Exceptions;
using Draya.Domain.Wallets;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Draya.Infrastructure.Identity.Services;

public class IdentityService : IIdentityService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly ITeacherRepository _teacherRepository;
    private readonly IStudentRepository _studentRepository;
    private readonly IPlatformAdminRepository _platformAdminRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ITeacherWalletRepository _teacherWalletRepository;
    private readonly ITokenService _tokenService;
    private readonly IEmailService _emailService;
    private readonly ILogger<IdentityService> _logger;

    public IdentityService(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        ITeacherRepository teacherRepository,
        IStudentRepository studentRepository,
        IPlatformAdminRepository platformAdminRepository,
        IRefreshTokenRepository refreshTokenRepository,
        ITeacherWalletRepository teacherWalletRepository,
        ITokenService tokenService,
        IEmailService emailService,
        ILogger<IdentityService> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _teacherRepository = teacherRepository;
        _studentRepository = studentRepository;
        _platformAdminRepository = platformAdminRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _teacherWalletRepository = teacherWalletRepository;
        _tokenService = tokenService;
        _emailService = emailService;
        _logger = logger;
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
        string parentGuardianName,
        string parentGuardianPhone,
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
            ParentGuardianName = parentGuardianName.Trim(),
            ParentGuardianPhone = parentGuardianPhone.Trim(),
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
            var roles = await _userManager.GetRolesAsync(user);
            _logger.LogInformation(
                "Password reset requested for {Email}. User roles: {Roles}",
                user.Email,
                string.Join(",", roles));

            var primaryRole = roles.FirstOrDefault() ?? "Student";
            await _emailService.SendPasswordResetEmailAsync(user.Email!, tokenValue, primaryRole, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Password reset email failed for {Email}. Verify Email:Host, Email:Port, Email:From, Email:UserName/Password, provider rate limits, and recipient-domain policy.",
                user.Email);
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
                student.ParentGuardianName,
                student.ParentGuardianPhone,
                student.DateOfBirth,
                student.ProfilePictureUrl);
        }

        return new UserSummaryDto(user.Id, "Admin", primaryRole);
    }


    public async Task UpdateAdminProfileAsync(Guid userId, string fullName, string email, string? phoneNumber, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            throw new UnauthorizedAccessException("User not found.");

        if (!string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase))
        {
            var existingUser = await _userManager.FindByEmailAsync(email);
            if (existingUser is not null && existingUser.Id != userId)
                throw new DuplicateEmailException(email);
                
            user.Email = email;
            user.UserName = email;
        }

        if (phoneNumber is not null)
            user.PhoneNumber = phoneNumber;

        await _userManager.UpdateAsync(user);

        var admin = await _platformAdminRepository.GetByUserIdAsync(userId, cancellationToken);
        if (admin is not null)
        {
            admin.FullName = fullName;
            await _platformAdminRepository.UpdateAsync(admin, cancellationToken);
            await _platformAdminRepository.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<SupervisorDto> InviteSupervisorAsync(string name, string email, string role, CancellationToken cancellationToken)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var existingUser = await _userManager.FindByEmailAsync(normalizedEmail);
        if (existingUser is not null)
            throw new DuplicateEmailException(email);

        await EnsureRoleExistsAsync(role);

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = normalizedEmail,
            Email = normalizedEmail,
            IsActive = false,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user);
        if (!result.Succeeded)
            throw new InvalidOperationException("Failed to create supervisor user.");

        await _userManager.AddToRoleAsync(user, role);

        var admin = new PlatformAdmin
        {
            UserId = user.Id,
            FullName = name.Trim(),
            CreatedAt = DateTime.UtcNow
        };
        await _platformAdminRepository.AddAsync(admin, cancellationToken);
        await _platformAdminRepository.SaveChangesAsync(cancellationToken);

        var inviteToken = await _userManager.GeneratePasswordResetTokenAsync(user);
        await _emailService.SendSupervisorInviteEmailAsync(user.Email!, inviteToken, cancellationToken);

        return new SupervisorDto(user.Id, name, user.Email!, role, user.IsActive, "PendingActivation", DateTime.UtcNow, user.CreatedAt);
    }

    public async Task ResendSupervisorInviteAsync(Guid supervisorId, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(supervisorId.ToString());
        if (user is null)
            throw new InvalidOperationException("Supervisor not found.");

        var inviteToken = await _userManager.GeneratePasswordResetTokenAsync(user);
        await _emailService.SendSupervisorInviteEmailAsync(user.Email!, inviteToken, cancellationToken);
    }

    public async Task AcceptSupervisorInviteAsync(string email, string token, string newPassword, CancellationToken cancellationToken)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var user = await _userManager.FindByEmailAsync(normalizedEmail);
        if (user is null)
            throw new InvalidOperationException("User not found.");

        var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
        if (!result.Succeeded)
            throw new InvalidPasswordResetTokenException();

        user.IsActive = true;
        await _userManager.UpdateAsync(user);
    }

    public async Task<List<SupervisorDto>> GetSupervisorsAsync(CancellationToken cancellationToken)
    {
        var admins = await _platformAdminRepository.GetAllAsync(cancellationToken);
        var supervisorDtos = new List<SupervisorDto>();

        foreach (var admin in admins)
        {
            var user = await _userManager.FindByIdAsync(admin.UserId.ToString());
            if (user is not null)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var role = roles.FirstOrDefault() ?? "Admin";
                var status = user.IsActive ? "Active" : "Inactive";
                supervisorDtos.Add(new SupervisorDto(user.Id, admin.FullName, user.Email!, role, user.IsActive, status, user.CreatedAt, user.CreatedAt));
            }
        }

        return supervisorDtos;
    }

    public async Task ToggleSupervisorStatusAsync(Guid supervisorId, bool isActive, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(supervisorId.ToString());
        if (user is null)
            throw new InvalidOperationException("Supervisor not found.");

        user.IsActive = isActive;
        await _userManager.UpdateAsync(user);
    }

    public async Task<List<TeacherSearchDto>> SearchTeachersForAdminAsync(string? query, CancellationToken cancellationToken)
    {
        var teachers = await _teacherRepository.GetAllAsync(cancellationToken);
        
        if (!string.IsNullOrWhiteSpace(query))
        {
            teachers = teachers.Where(t => t.FullName.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        var results = new List<TeacherSearchDto>();

        foreach (var teacher in teachers)
        {
            var user = await _userManager.FindByIdAsync(teacher.UserId.ToString());
            if (user is null) continue;

            if (!string.IsNullOrWhiteSpace(query) && 
                !teacher.FullName.Contains(query, StringComparison.OrdinalIgnoreCase) &&
                !user.Email!.Contains(query, StringComparison.OrdinalIgnoreCase))
            {
                continue; // filter by email if full name didn't match
            }

            var wallet = await _teacherWalletRepository.GetByTeacherIdAsync(teacher.UserId, cancellationToken);
            var earnedBalance = wallet?.EarnedBalance ?? 0m;
            var purchasedBalance = wallet?.PurchasedBalance ?? 0m;

            results.Add(new TeacherSearchDto(teacher.UserId, teacher.FullName, user.Email!, earnedBalance, purchasedBalance));
        }

        return results;
    }

    public async Task<(List<AdminStudentDto> Items, int TotalCount)> SearchStudentsForAdminAsync(string? query, int page, int pageSize, CancellationToken cancellationToken)
    {
        var queryable = _studentRepository.GetQueryable();

        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim().ToLower();
            queryable = queryable.Where(s => 
                s.FullName.ToLower().Contains(term) || 
                s.ParentGuardianEmail.ToLower().Contains(term) ||
                s.ParentGuardianName.ToLower().Contains(term) ||
                s.ParentGuardianPhone.ToLower().Contains(term));
        }

        var totalCount = await queryable.CountAsync(cancellationToken);
        
        var students = await queryable
            .OrderBy(s => s.FullName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = new List<AdminStudentDto>();

        foreach (var student in students)
        {
            var user = await _userManager.FindByIdAsync(student.UserId.ToString());
            if (user == null) continue;

            items.Add(new AdminStudentDto(
                student.UserId,
                student.FullName,
                user.Email ?? string.Empty,
                student.ParentGuardianEmail,
                student.ParentGuardianName,
                student.ParentGuardianPhone,
                student.DateOfBirth,
                user.IsActive,
                user.CreatedAt
            ));
        }

        return (items, totalCount);
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
