using Draya.Domain.Identity;
using Draya.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Draya.Infrastructure.Persistence.Configurations;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(u => u.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(u => u.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(u => u.LastLoginAt);

        builder.HasOne(u => u.Teacher)
            .WithOne()
            .HasForeignKey<Teacher>(t => t.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(u => u.Student)
            .WithOne()
            .HasForeignKey<Student>(s => s.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(u => u.PlatformAdmin)
            .WithOne()
            .HasForeignKey<PlatformAdmin>(a => a.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
