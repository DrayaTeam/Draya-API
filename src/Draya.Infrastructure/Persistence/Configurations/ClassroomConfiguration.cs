using Draya.Domain.Classrooms;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Draya.Infrastructure.Persistence.Configurations;

public class ClassroomConfiguration : IEntityTypeConfiguration<Classroom>
{
    public void Configure(EntityTypeBuilder<Classroom> builder)
    {
        builder.ToTable("Classrooms");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.EnrollmentCode)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(c => c.IsActive)
            .HasDefaultValue(true);

        builder.Property(c => c.CreatedAt)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(c => c.TeacherId)
            .HasDatabaseName("IX_Classroom_TeacherId");

        builder.HasIndex(c => c.EnrollmentCode)
            .IsUnique()
            .HasDatabaseName("UQ_Classroom_EnrollmentCode");

        builder.HasOne(c => c.Subject)
            .WithMany()
            .HasForeignKey(c => c.SubjectId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
