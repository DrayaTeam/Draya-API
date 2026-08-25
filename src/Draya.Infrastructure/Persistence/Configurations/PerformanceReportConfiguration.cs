using Draya.Domain.Reports;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Draya.Infrastructure.Persistence.Configurations;

public class PerformanceReportConfiguration : IEntityTypeConfiguration<PerformanceReport>
{
    public void Configure(EntityTypeBuilder<PerformanceReport> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.TeacherName).IsRequired().HasMaxLength(255);

        builder.Property(x => x.SummaryText)
            .IsRequired();

        builder.Property(x => x.AverageExamDurationMinutes)
            .HasColumnType("decimal(18,2)");

        builder.Property(x => x.ClassroomPercentile)
            .HasColumnType("decimal(18,2)");

        // Use JSON columns for the collections to keep it simple and denormalized
        builder.OwnsMany(x => x.SubjectProficiencies, sp =>
        {
            sp.ToJson();
            sp.Property(p => p.SubjectName).IsRequired();
            sp.Property(p => p.ProficiencyPercent).HasColumnType("decimal(18,2)");
        });

        builder.OwnsMany(x => x.TrendPoints, tp =>
        {
            tp.ToJson();
            tp.Property(p => p.AverageScore).HasColumnType("decimal(18,2)");
        });

        builder.OwnsMany(x => x.WeakTopics, wt =>
        {
            wt.ToJson();
            wt.Property(p => p.TopicName).IsRequired();
            wt.Property(p => p.SubjectName).IsRequired();
            wt.Property(p => p.ProficiencyPercent).HasColumnType("decimal(18,2)");
            wt.Property(p => p.Status).IsRequired();
            wt.Property(p => p.Recommendation).IsRequired();
        });
    }
}
