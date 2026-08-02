using Draya.Application.Common.Interfaces;
using Draya.Domain.Identity;
using Microsoft.AspNetCore.Identity;

namespace Draya.Infrastructure.Identity.Services;

/// <summary>
/// Adapts ASP.NET Core Identity's versioned password-hash format behind the
/// Application-layer abstraction.
/// </summary>
public class PasswordHasher : IPasswordHasher
{
    private readonly Microsoft.AspNetCore.Identity.PasswordHasher<AppUser> _hasher = new();

    public string HashPassword(string password)
        => _hasher.HashPassword(new AppUser(), password);

    public bool VerifyPassword(string password, string passwordHash)
        => _hasher.VerifyHashedPassword(new AppUser(), passwordHash, password)
           != PasswordVerificationResult.Failed;
}
