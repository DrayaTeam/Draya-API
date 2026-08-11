using Draya.Domain.Admin;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Draya.Infrastructure.Persistence.Configurations;

public class PlatformSettingConfiguration : IEntityTypeConfiguration<PlatformSetting>
{
    public static readonly Guid DefaultSettingsId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public void Configure(EntityTypeBuilder<PlatformSetting> b)
    {
        b.ToTable("PlatformSettings", t =>
        {
            t.HasCheckConstraint("CK_PlatformSetting_AIExamPrice", "[AIExamPrice] >= 0");
            t.HasCheckConstraint("CK_PlatformSetting_FreeMonthlyAIExamQuota", "[FreeMonthlyAIExamQuota] >= 0");
            t.HasCheckConstraint("CK_PlatformSetting_PlatformCommissionPercent", "[PlatformCommissionPercent] >= 0 AND [PlatformCommissionPercent] <= 100");
        });

        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.AIExamPrice).HasPrecision(18, 2).HasDefaultValue(20.00m);
        b.Property(x => x.FreeMonthlyAIExamQuota).HasDefaultValue(3);
        b.Property(x => x.PlatformCommissionPercent).HasPrecision(5, 2).HasDefaultValue(5.00m);
        b.Property(x => x.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        b.Property(x => x.UpdatedByAdminId).IsRequired(false);

        // Seed single row configuration
        b.HasData(new PlatformSetting
        {
            Id = DefaultSettingsId,
            AIExamPrice = 20.00m,
            FreeMonthlyAIExamQuota = 3,
            PlatformCommissionPercent = 5.00m,
            UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            UpdatedByAdminId = null
        });
    }
}
