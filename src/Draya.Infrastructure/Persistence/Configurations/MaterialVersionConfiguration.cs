using Draya.Domain.Materials;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Draya.Infrastructure.Persistence.Configurations;

public class MaterialVersionConfiguration : IEntityTypeConfiguration<MaterialVersion>
{
    public void Configure(EntityTypeBuilder<MaterialVersion> builder)
    {
        builder.HasQueryFilter(x => !x.Material.IsDeleted);
    }
}
