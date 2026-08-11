namespace Draya.Domain.Admin;

public interface IPlatformSettingRepository
{
    Task<PlatformSetting> GetSettingsAsync(CancellationToken cancellationToken = default);
    Task UpdateSettingsAsync(PlatformSetting settings, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
