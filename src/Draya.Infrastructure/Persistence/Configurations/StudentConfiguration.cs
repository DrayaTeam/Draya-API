using Draya.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Draya.Infrastructure.Persistence.Configurations;

public class StudentConfiguration : IEntityTypeConfiguration<Student>
{
    public void Configure(EntityTypeBuilder<Student> builder)
    {
        builder.ToTable("Students");

        builder.HasKey(s => s.UserId);

        builder.Property(s => s.UserId)
            .ValueGeneratedNever();

        builder.Property(s => s.FullName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(s => s.ParentGuardianEmail)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(s => s.ParentGuardianName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(s => s.ParentGuardianPhone)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(s => s.DateOfBirth)
            .HasColumnType("date");

        builder.Property(s => s.ProfilePictureUrl)
            .HasMaxLength(1000);

        builder.Property(s => s.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("SYSUTCDATETIME()");
    }
}

