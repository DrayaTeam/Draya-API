using Draya.Domain.Exams;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Draya.Infrastructure.Persistence.Configurations;

public class AnswerGradingResultConfiguration : IEntityTypeConfiguration<AnswerGradingResult>
{
    public void Configure(EntityTypeBuilder<AnswerGradingResult> builder)
    {
        builder.ToTable("AnswerGradingResults");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Score).HasColumnType("decimal(18,2)");
        builder.Property(x => x.MaxScore).HasColumnType("decimal(18,2)");
        builder.Property(x => x.ConfidenceScore).HasColumnType("decimal(18,2)");
        builder.Property(x => x.TeacherOverrideScore).HasColumnType("decimal(18,2)");
    }
}
