using Draya.Domain.Exams;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Draya.Infrastructure.Persistence.Configurations;

public class StudentExamAttemptConfiguration : IEntityTypeConfiguration<StudentExamAttempt>
{
    public void Configure(EntityTypeBuilder<StudentExamAttempt> builder)
    {
        builder.ToTable("StudentExamAttempts");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.FinalScore)
            .HasColumnType("decimal(18,2)");

        builder.HasMany(x => x.Answers)
            .WithOne()
            .HasForeignKey(x => x.StudentExamAttemptId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
