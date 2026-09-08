using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace Draya.Infrastructure.Identity;

public class CustomOtpTokenProvider<TUser> : TotpSecurityStampBasedTokenProvider<TUser> where TUser : class
{
    public override Task<bool> CanGenerateTwoFactorTokenAsync(UserManager<TUser> manager, TUser user)
    {
        return Task.FromResult(false);
    }

    public override async Task<string> GenerateAsync(string purpose, UserManager<TUser> manager, TUser user)
    {
        var token = await base.GenerateAsync(purpose, manager, user);
        return token;
    }

    public override async Task<bool> ValidateAsync(string purpose, string token, UserManager<TUser> manager, TUser user)
    {
        return await base.ValidateAsync(purpose, token, manager, user);
    }
}
