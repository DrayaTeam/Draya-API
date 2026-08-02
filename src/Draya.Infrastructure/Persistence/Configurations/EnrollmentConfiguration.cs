using Draya.Domain.Classrooms;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Draya.Infrastructure.Persistence.Configurations;

public class EnrollmentConfiguration : IEntityTypeConfiguration<Enrollment>
{
    public void Configure(EntityTypeBuilder<Enrollment> builder)
    {
        builder.ToTable("Enrollments");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(EnrollmentStatus.Active);

        builder.Property(e => e.EnrolledAt)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(e => new { e.StudentId, e.ClassroomId })
            .IsUnique()
            .HasDatabaseName("UQ_Enrollment_StudentId_ClassroomId");

        builder.HasIndex(e => e.ClassroomId)
            .HasDatabaseName("IX_Enrollment_ClassroomId");
    }
}
