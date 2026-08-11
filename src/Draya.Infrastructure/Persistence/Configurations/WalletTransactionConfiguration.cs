using Draya.Domain.Wallets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Draya.Infrastructure.Persistence.Configurations;

public class WalletTransactionConfiguration : IEntityTypeConfiguration<WalletTransaction>
{
    public void Configure(EntityTypeBuilder<WalletTransaction> b)
    {
        b.ToTable("WalletTransactions");

        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.TeacherId).IsRequired();
        b.Property(x => x.Type).HasConversion<string>().HasMaxLength(30).IsRequired();
        b.Property(x => x.Amount).HasPrecision(18, 2).IsRequired();
        b.Property(x => x.BalanceType).HasConversion<string>().HasMaxLength(10).IsRequired();
        b.Property(x => x.ReferenceId).IsRequired(false);
        b.Property(x => x.Description).HasMaxLength(500);
        b.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

        b.HasIndex(x => x.TeacherId).HasDatabaseName("IX_WalletTransaction_TeacherId");
        b.HasIndex(x => x.CreatedAt).HasDatabaseName("IX_WalletTransaction_CreatedAt");
    }
}
