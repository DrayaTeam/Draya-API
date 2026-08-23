using Draya.Domain.Reports;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Draya.Infrastructure.Persistence.Configurations;

public class WeaknessReviewConfiguration : IEntityTypeConfiguration<WeaknessReview>
{
    public void Configure(EntityTypeBuilder<WeaknessReview> builder)
    {
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.ProficiencyAtGeneration)
            .HasPrecision(5, 2);

        // Required fields
        builder.Property(x => x.AiExplanation).IsRequired();

        // Foreign Key
        builder.HasOne<StudentWeakness>()
            .WithMany()
            .HasForeignKey(x => x.StudentWeaknessId)
            .OnDelete(DeleteBehavior.Cascade);

        // Filtered unique index: Only one active/current review per weakness at a time
        builder.HasIndex(x => x.StudentWeaknessId)
            .IsUnique()
            .HasFilter("[IsOutdated] = 0");
    }
}
