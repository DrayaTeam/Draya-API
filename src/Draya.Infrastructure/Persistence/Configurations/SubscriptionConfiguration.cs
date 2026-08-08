using Draya.Domain.Subscriptions;
using Draya.Domain.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Draya.Infrastructure.Persistence.Configurations;

public class SubscriptionPlanConfiguration : IEntityTypeConfiguration<SubscriptionPlan>
{
    public void Configure(EntityTypeBuilder<SubscriptionPlan> b)
    {
        b.ToTable("SubscriptionPlans", t =>
        {
            t.HasCheckConstraint("CK_SubscriptionPlan_MaxStudents", "[MaxStudents] > 0");
            t.HasCheckConstraint("CK_SubscriptionPlan_MaxStorageMB", "[MaxStorageMB] > 0");
            t.HasCheckConstraint("CK_SubscriptionPlan_MonthlyExamQuota", "[MonthlyExamQuota] > 0");
            t.HasCheckConstraint("CK_SubscriptionPlan_PriceMonthly", "[PriceMonthly] >= 0");
        });

        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Name).IsRequired().HasMaxLength(100);
        b.HasIndex(x => x.Name).IsUnique();
        b.Property(x => x.PriceMonthly).HasPrecision(10,2).HasDefaultValue(0m);
        b.Property(x => x.IsActive).HasDefaultValue(true);
        b.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

        // Seed Free subscription plan with deterministic values
        b.HasData(new SubscriptionPlan
        {
            Id = SubscriptionPlanIds.FreePlanId,
            Name = "Free",
            MaxStudents = 30,
            MaxStorageMB = 500,
            MonthlyExamQuota = 3,
            PriceMonthly = 0m,
            IsActive = true,
            CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });
    }
}

public class TeacherSubscriptionConfiguration : IEntityTypeConfiguration<TeacherSubscription> { public void Configure(EntityTypeBuilder<TeacherSubscription> b) { b.ToTable("TeacherSubscriptions", t => t.HasCheckConstraint("CK_TeacherSubscription_Status", "[Status] IN ('Active', 'Expired', 'Cancelled')")); b.HasKey(x => x.Id); b.Property(x => x.Id).ValueGeneratedNever(); b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).HasDefaultValue(SubscriptionStatus.Active); b.Property(x => x.StartDate).HasDefaultValueSql("SYSUTCDATETIME()"); b.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()"); b.HasIndex(x => new { x.TeacherId, x.Status }).HasDatabaseName("IX_TeacherSubscription_TeacherId_Status"); b.HasOne(x => x.Plan).WithMany().HasForeignKey(x => x.PlanId).OnDelete(DeleteBehavior.Restrict); } }
public class UsageCounterConfiguration : IEntityTypeConfiguration<UsageCounter> { public void Configure(EntityTypeBuilder<UsageCounter> b) { b.ToTable("UsageCounters", t => { t.HasCheckConstraint("CK_UsageCounter_CurrentStudentsCount", "[CurrentStudentsCount] >= 0"); t.HasCheckConstraint("CK_UsageCounter_ExamsGeneratedCount", "[ExamsGeneratedCount] >= 0"); t.HasCheckConstraint("CK_UsageCounter_StorageUsedMB", "[StorageUsedMB] >= 0"); }); b.HasKey(x => x.Id); b.Property(x => x.Id).ValueGeneratedNever(); b.Property(x => x.PeriodMonth).HasColumnType("date"); b.Property(x => x.CurrentStudentsCount).HasDefaultValue(0); b.Property(x => x.ExamsGeneratedCount).HasDefaultValue(0); b.Property(x => x.StorageUsedMB).HasPrecision(10,2).HasDefaultValue(0m); b.HasIndex(x => new { x.TeacherId, x.PeriodMonth }).IsUnique().HasDatabaseName("UQ_UsageCounter_TeacherId_PeriodMonth"); } }
