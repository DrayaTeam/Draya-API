using Draya.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Draya.Infrastructure.Persistence.Configurations;

public class PasswordResetTokenConfiguration : IEntityTypeConfiguration<PasswordResetToken>
{
    public void Configure(EntityTypeBuilder<PasswordResetToken> builder)
    {
        builder.ToTable("PasswordResetTokens");
        builder.HasKey(token => token.Id);
        builder.Property(token => token.Id).ValueGeneratedNever();
        builder.Property(token => token.TokenHash).IsRequired().HasMaxLength(256);
        builder.HasIndex(token => token.TokenHash).IsUnique().HasDatabaseName("UQ_PasswordResetToken_TokenHash");
        builder.HasIndex(token => token.UserId).HasDatabaseName("IX_PasswordResetToken_UserId");
        builder.Property(token => token.ExpiresAt).IsRequired();
        builder.Property(token => token.CreatedAt).IsRequired().HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Ignore(token => token.IsActive);
        builder.HasOne(token => token.User)
            .WithMany()
            .HasForeignKey(token => token.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
