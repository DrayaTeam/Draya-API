using Draya.Domain.Subscriptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Draya.Infrastructure.Persistence.Configurations;

public class UsageCounterConfiguration : IEntityTypeConfiguration<UsageCounter>
{
    public void Configure(EntityTypeBuilder<UsageCounter> b)
    {
        b.ToTable("UsageCounters", t =>
        {
            t.HasCheckConstraint("CK_UsageCounter_FreeExamsUsed", "[FreeExamsUsed] >= 0");
            t.HasCheckConstraint("CK_UsageCounter_PaidExamsGenerated", "[PaidExamsGenerated] >= 0");
        });

        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.PeriodMonth).HasColumnType("date");
        b.Property(x => x.FreeExamsUsed).HasDefaultValue(0);
        b.Property(x => x.PaidExamsGenerated).HasDefaultValue(0);
        b.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

        b.HasIndex(x => new { x.TeacherId, x.PeriodMonth })
            .IsUnique()
            .HasDatabaseName("UQ_UsageCounter_TeacherId_PeriodMonth");
    }
}
