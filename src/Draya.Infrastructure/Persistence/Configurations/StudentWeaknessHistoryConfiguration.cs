using Draya.Domain.Reports;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Draya.Infrastructure.Persistence.Configurations;

public class StudentWeaknessHistoryConfiguration : IEntityTypeConfiguration<StudentWeaknessHistory>
{
    public void Configure(EntityTypeBuilder<StudentWeaknessHistory> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.PreviousProficiencyPercent)
            .HasPrecision(5, 2);

        builder.Property(x => x.NewProficiencyPercent)
            .HasPrecision(5, 2);

        builder.HasOne<StudentWeakness>()
            .WithMany()
            .HasForeignKey(x => x.StudentWeaknessId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasOne<Draya.Domain.Exams.StudentExamAttempt>()
            .WithMany()
            .HasForeignKey(x => x.SourceAttemptId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
