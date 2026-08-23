using Draya.Domain.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Draya.Infrastructure.Persistence.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");

        builder.HasKey(n => n.Id);

        builder.Property(n => n.Title)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(n => n.Message)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(n => n.Type)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(n => n.Link)
            .HasMaxLength(255);

        builder.HasIndex(n => n.UserId);
        builder.HasIndex(n => n.Read);
        builder.HasIndex(n => n.CreatedAt);

    }
}
