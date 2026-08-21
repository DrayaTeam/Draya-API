using Draya.Domain.Reports;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Draya.Infrastructure.Persistence.Configurations;

public class PerformanceReportConfiguration : IEntityTypeConfiguration<PerformanceReport>
{
    public void Configure(EntityTypeBuilder<PerformanceReport> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.SummaryText)
            .IsRequired();

        // Use JSON columns for the collections to keep it simple and denormalized
        builder.OwnsMany(x => x.SubjectProficiencies, sp =>
        {
            sp.ToJson();
            sp.Property(p => p.SubjectName).IsRequired();
        });

        builder.OwnsMany(x => x.TrendPoints, tp =>
        {
            tp.ToJson();
        });

        builder.OwnsMany(x => x.WeakTopics, wt =>
        {
            wt.ToJson();
            wt.Property(p => p.TopicName).IsRequired();
            wt.Property(p => p.SubjectName).IsRequired();
            wt.Property(p => p.Status).IsRequired();
            wt.Property(p => p.Recommendation).IsRequired();
        });
    }
}
