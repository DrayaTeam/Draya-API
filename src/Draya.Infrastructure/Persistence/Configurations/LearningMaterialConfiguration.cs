using Draya.Domain.Materials;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Draya.Infrastructure.Persistence.Configurations;

public class LearningMaterialConfiguration : IEntityTypeConfiguration<LearningMaterial>
{
    public void Configure(EntityTypeBuilder<LearningMaterial> builder)
    {
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Title)
            .IsRequired()
            .HasMaxLength(300);

        builder.HasOne(x => x.Section)
            .WithMany(s => s.Materials)
            .HasForeignKey(x => x.SectionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Soft delete query filter
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
