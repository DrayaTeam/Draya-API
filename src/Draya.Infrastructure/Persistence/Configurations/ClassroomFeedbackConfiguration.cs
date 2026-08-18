using Draya.Domain.Classrooms;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Draya.Infrastructure.Persistence.Configurations;

public class ClassroomFeedbackConfiguration : IEntityTypeConfiguration<ClassroomFeedback>
{
    public void Configure(EntityTypeBuilder<ClassroomFeedback> builder)
    {
        builder.ToTable("ClassroomFeedback");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.Rating)
            .IsRequired();

        builder.Property(f => f.Comment)
            .HasMaxLength(1000);

        builder.Property(f => f.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(f => new { f.ClassroomId, f.StudentId })
            .IsUnique()
            .HasDatabaseName("UQ_ClassroomFeedback_ClassroomId_StudentId");

        builder.HasIndex(f => f.ClassroomId)
            .HasDatabaseName("IX_ClassroomFeedback_ClassroomId");

        builder.HasOne(f => f.Classroom)
            .WithMany()
            .HasForeignKey(f => f.ClassroomId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_ClassroomFeedback_Rating", "[Rating] >= 1 AND [Rating] <= 5");
        });
    }
}
