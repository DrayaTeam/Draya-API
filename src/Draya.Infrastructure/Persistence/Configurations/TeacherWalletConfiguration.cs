using Draya.Domain.Wallets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Draya.Infrastructure.Persistence.Configurations;

public class TeacherWalletConfiguration : IEntityTypeConfiguration<TeacherWallet>
{
    public void Configure(EntityTypeBuilder<TeacherWallet> b)
    {
        b.ToTable("TeacherWallets", t =>
        {
            t.HasCheckConstraint("CK_TeacherWallet_EarnedBalance", "[EarnedBalance] >= 0");
            t.HasCheckConstraint("CK_TeacherWallet_PurchasedBalance", "[PurchasedBalance] >= 0");
        });

        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.HasIndex(x => x.TeacherId).IsUnique().HasDatabaseName("UQ_TeacherWallet_TeacherId");
        b.Property(x => x.EarnedBalance).HasPrecision(18, 2).HasDefaultValue(0m);
        b.Property(x => x.PurchasedBalance).HasPrecision(18, 2).HasDefaultValue(0m);
        b.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
    }
}
