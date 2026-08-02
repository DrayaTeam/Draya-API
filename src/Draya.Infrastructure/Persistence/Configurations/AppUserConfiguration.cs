using Draya.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Draya.Infrastructure.Persistence.Configurations;

public class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        builder.ToTable("AppUsers", table =>
            table.HasCheckConstraint("CK_AppUser_Role", "[Role] IN ('Teacher', 'Student', 'SuperAdmin')"));

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Id)
            .ValueGeneratedNever();

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(256);

        builder.HasIndex(u => u.Email)
            .IsUnique()
            .HasDatabaseName("UQ_AppUser_Email");

        builder.HasIndex(u => u.Role)
            .HasDatabaseName("IX_AppUser_Role");

        builder.Property(u => u.PasswordHash)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(u => u.Role)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(u => u.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(u => u.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(u => u.LastLoginAt);

        builder.Property(u => u.FailedLoginAttempts)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(u => u.LockoutEndUtc);

        // 1:1 navigation to extension tables
        builder.HasOne(u => u.Teacher)
            .WithOne(t => t.AppUser)
            .HasForeignKey<Teacher>(t => t.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(u => u.Student)
            .WithOne(s => s.AppUser)
            .HasForeignKey<Student>(s => s.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(u => u.PlatformAdmin)
            .WithOne(a => a.AppUser)
            .HasForeignKey<PlatformAdmin>(a => a.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
