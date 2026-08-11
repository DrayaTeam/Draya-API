using Draya.Domain.Admin;
using Draya.Infrastructure.Persistence;
using Draya.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Draya.Infrastructure.Admin;

public class PlatformSettingRepository : IPlatformSettingRepository
{
    private readonly ApplicationDbContext _context;

    public PlatformSettingRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PlatformSetting> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _context.PlatformSettings.FirstOrDefaultAsync(cancellationToken);
        if (settings == null)
        {
            settings = new PlatformSetting
            {
                Id = PlatformSettingConfiguration.DefaultSettingsId,
                AIExamPrice = 20.00m,
                FreeMonthlyAIExamQuota = 3,
                PlatformCommissionPercent = 5.00m,
                UpdatedAt = DateTime.UtcNow
            };
            await _context.PlatformSettings.AddAsync(settings, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }
        return settings;
    }

    public Task UpdateSettingsAsync(PlatformSetting settings, CancellationToken cancellationToken = default)
    {
        settings.UpdatedAt = DateTime.UtcNow;
        _context.PlatformSettings.Update(settings);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
