using Draya.Domain.Classrooms;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Draya.Infrastructure.Persistence.Configurations;

public class ClassroomTypeConfiguration : IEntityTypeConfiguration<ClassroomType>
{
    public void Configure(EntityTypeBuilder<ClassroomType> builder)
    {
        builder.ToTable("ClassroomTypes");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.Description)
            .HasMaxLength(500);

        builder.Property(c => c.IsActive)
            .HasDefaultValue(true);

        builder.Property(c => c.CreatedAt)
            .HasDefaultValueSql("SYSUTCDATETIME()");
    }
}
