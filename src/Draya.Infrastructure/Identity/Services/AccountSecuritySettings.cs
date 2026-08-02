using Draya.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Draya.Infrastructure.Identity.Services;

public class AccountSecuritySettings : IAccountSecuritySettings
{
    public AccountSecuritySettings(IConfiguration configuration)
    {
        var section = configuration.GetSection("AccountSecurity");
        LockoutThreshold = section.GetValue<int>("LockoutThreshold");
        LockoutCooldownMinutes = section.GetValue<int>("LockoutCooldownMinutes");
        PasswordResetTokenExpiryMinutes = section.GetValue<int>("PasswordResetTokenExpiryMinutes");

        if (LockoutThreshold <= 0 || LockoutCooldownMinutes <= 0 || PasswordResetTokenExpiryMinutes <= 0)
            throw new InvalidOperationException("Account security settings must be positive values.");
    }

    public int LockoutThreshold { get; }
    public int LockoutCooldownMinutes { get; }
    public int PasswordResetTokenExpiryMinutes { get; }
}
