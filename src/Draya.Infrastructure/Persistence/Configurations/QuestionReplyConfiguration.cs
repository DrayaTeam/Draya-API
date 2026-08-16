using Draya.Domain.Classrooms;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Draya.Infrastructure.Persistence.Configurations;

public class QuestionReplyConfiguration : IEntityTypeConfiguration<QuestionReply>
{
    public void Configure(EntityTypeBuilder<QuestionReply> builder)
    {
        builder.ToTable("QuestionReplies");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Content)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(r => r.ImageUrl)
            .HasMaxLength(1000);

        builder.HasIndex(r => r.QuestionId);
        builder.HasIndex(r => r.AuthorId);
        
        // Ensure only one official teacher answer per question
        builder.HasIndex(r => new { r.QuestionId, r.IsTeacherAnswer })
            .IsUnique()
            .HasFilter("[IsTeacherAnswer] = 1");
    }
}
