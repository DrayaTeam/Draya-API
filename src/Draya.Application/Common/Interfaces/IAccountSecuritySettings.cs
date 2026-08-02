namespace Draya.Application.Common.Interfaces;

public interface IAccountSecuritySettings
{
    int LockoutThreshold { get; }
    int LockoutCooldownMinutes { get; }
    int PasswordResetTokenExpiryMinutes { get; }
}
