using Draya.Domain.Exams;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Draya.Infrastructure.Persistence.Configurations;

public class ExamGenerationConfiguration : IEntityTypeConfiguration<ExamGeneration>
{
    public void Configure(EntityTypeBuilder<ExamGeneration> builder)
    {
        builder.ToTable("ExamGenerations");
        builder.HasKey(x => x.Id);
        
        builder.HasIndex(x => x.IdempotencyKey).IsUnique();
        
        builder.Property(x => x.IdempotencyKey).IsRequired().HasMaxLength(128);
        builder.Property(x => x.Status).HasConversion<string>().IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
    }
}
