using Draya.Domain.Exams;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Draya.Infrastructure.Persistence.Configurations;

public class ExamGradingJobConfiguration : IEntityTypeConfiguration<ExamGradingJob>
{
    public void Configure(EntityTypeBuilder<ExamGradingJob> builder)
    {
        builder.ToTable("ExamGradingJobs");

        builder.HasKey(x => x.Id);
        
        builder.HasIndex(x => x.IdempotencyKey).IsUnique();
        
        builder.Property(x => x.Status).HasConversion<string>();
    }
}
