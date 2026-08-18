using Draya.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Draya.Infrastructure.Persistence.Configurations;

public class PiiMappingConfiguration : IEntityTypeConfiguration<PiiMapping>
{
    public void Configure(EntityTypeBuilder<PiiMapping> builder)
    {
        builder.ToTable("PiiMappings");
        builder.HasKey(x => x.StudentId);
        
        builder.HasIndex(x => x.AnonymizedId).IsUnique();

        builder.Property(x => x.CreatedAt).IsRequired();
    }
}
