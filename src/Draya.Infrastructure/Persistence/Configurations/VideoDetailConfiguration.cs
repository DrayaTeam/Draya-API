using Draya.Domain.Materials;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Draya.Infrastructure.Persistence.Configurations;

public class VideoDetailConfiguration : IEntityTypeConfiguration<VideoDetail>
{
    public void Configure(EntityTypeBuilder<VideoDetail> builder)
    {
        builder.HasQueryFilter(x => !x.Material.IsDeleted);
    }
}
