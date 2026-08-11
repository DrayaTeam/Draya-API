using Draya.Domain.Payments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Draya.Infrastructure.Persistence.Configurations;

public class PaymentTransactionConfiguration : IEntityTypeConfiguration<PaymentTransaction>
{
    public void Configure(EntityTypeBuilder<PaymentTransaction> b)
    {
        b.ToTable("PaymentTransactions", t =>
        {
            t.HasCheckConstraint("CK_PaymentTransaction_GrossAmount", "[GrossAmount] >= 0");
        });

        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Purpose).HasConversion<string>().HasMaxLength(30).IsRequired();
        b.Property(x => x.PayerId).IsRequired();
        b.Property(x => x.ClassroomId).IsRequired(false);
        b.Property(x => x.GrossAmount).HasPrecision(18, 2).IsRequired();
        b.Property(x => x.CommissionPercent).HasPrecision(5, 2).IsRequired(false);
        b.Property(x => x.CommissionAmount).HasPrecision(18, 2).IsRequired(false);
        b.Property(x => x.TeacherAmount).HasPrecision(18, 2).IsRequired(false);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        b.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

        b.HasIndex(x => x.PayerId).HasDatabaseName("IX_PaymentTransaction_PayerId");
        b.HasIndex(x => x.ClassroomId).HasDatabaseName("IX_PaymentTransaction_ClassroomId");
    }
}
