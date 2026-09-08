using Draya.Domain.Exams;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Draya.Infrastructure.Persistence.Configurations;

public class StudentAnswerConfiguration : IEntityTypeConfiguration<StudentAnswer>
{
    public void Configure(EntityTypeBuilder<StudentAnswer> builder)
    {
        builder.ToTable("StudentAnswers");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.AnswerText).HasMaxLength(4000);

        builder.HasOne(x => x.GradingResult)
            .WithOne()
            .HasForeignKey<AnswerGradingResult>(x => x.StudentAnswerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
