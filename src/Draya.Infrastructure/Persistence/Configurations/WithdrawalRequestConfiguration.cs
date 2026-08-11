using Draya.Domain.Wallets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Draya.Infrastructure.Persistence.Configurations;

public class WithdrawalRequestConfiguration : IEntityTypeConfiguration<WithdrawalRequest>
{
    public void Configure(EntityTypeBuilder<WithdrawalRequest> b)
    {
        b.ToTable("WithdrawalRequests", t =>
        {
            t.HasCheckConstraint("CK_WithdrawalRequest_Amount", "[Amount] > 0");
        });

        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.TeacherId).IsRequired();
        b.Property(x => x.Amount).HasPrecision(18, 2).IsRequired();
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        b.Property(x => x.RequestedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        b.Property(x => x.AdminNote).HasMaxLength(1000);
        b.Property(x => x.RejectionReason).HasMaxLength(500);

        b.HasIndex(x => x.TeacherId).HasDatabaseName("IX_WithdrawalRequest_TeacherId");
        b.HasIndex(x => x.Status).HasDatabaseName("IX_WithdrawalRequest_Status");
    }
}
