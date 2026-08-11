using Draya.Domain.Wallets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Draya.Infrastructure.Persistence.Configurations;

public class TeacherPayoutAccountConfiguration : IEntityTypeConfiguration<TeacherPayoutAccount>
{
    public void Configure(EntityTypeBuilder<TeacherPayoutAccount> b)
    {
        b.ToTable("TeacherPayoutAccounts");

        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.TeacherId).IsRequired();
        b.Property(x => x.AccountType).HasConversion<string>().HasMaxLength(20).IsRequired();
        b.Property(x => x.AccountName).IsRequired().HasMaxLength(200);
        b.Property(x => x.AccountIdentifier).IsRequired().HasMaxLength(200);
        b.Property(x => x.IsDefault).HasDefaultValue(false);
        b.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

        b.HasIndex(x => x.TeacherId).HasDatabaseName("IX_TeacherPayoutAccount_TeacherId");
    }
}
