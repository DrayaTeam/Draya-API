using Draya.Domain.Reports;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Draya.Infrastructure.Persistence.Configurations;

public class StudentWeaknessConfiguration : IEntityTypeConfiguration<StudentWeakness>
{
    public void Configure(EntityTypeBuilder<StudentWeakness> builder)
    {
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.TopicNameSnapshot)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(x => x.CurrentProficiencyPercent)
            .HasPrecision(5, 2);

        // Unique constraint on StudentId + TopicId
        builder.HasIndex(x => new { x.StudentId, x.TopicId }).IsUnique();
        
        builder.HasOne<Draya.Domain.Identity.Student>()
            .WithMany()
            .HasForeignKey(x => x.StudentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
