using Draya.Domain.Exams;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Draya.Infrastructure.Persistence.Configurations;

public class ExamConfiguration : IEntityTypeConfiguration<Exam>
{
    public void Configure(EntityTypeBuilder<Exam> builder)
    {
        builder.ToTable("Exams");
        builder.HasKey(x => x.Id);
        
        builder.HasMany(x => x.Questions)
               .WithOne()
               .HasForeignKey(x => x.ExamId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ExamQuestionConfiguration : IEntityTypeConfiguration<ExamQuestion>
{
    public void Configure(EntityTypeBuilder<ExamQuestion> builder)
    {
        builder.ToTable("ExamQuestions");
        builder.HasKey(x => x.Id);
        
        builder.HasMany(x => x.Options)
               .WithOne()
               .HasForeignKey(x => x.ExamQuestionId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ExamQuestionOptionConfiguration : IEntityTypeConfiguration<ExamQuestionOption>
{
    public void Configure(EntityTypeBuilder<ExamQuestionOption> builder)
    {
        builder.ToTable("ExamQuestionOptions");
        builder.HasKey(x => x.Id);
    }
}
