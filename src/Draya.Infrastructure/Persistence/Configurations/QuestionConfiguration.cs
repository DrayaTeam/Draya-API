using Draya.Domain.Classrooms;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Draya.Infrastructure.Persistence.Configurations;

public class QuestionConfiguration : IEntityTypeConfiguration<Question>
{
    public void Configure(EntityTypeBuilder<Question> builder)
    {
        builder.ToTable("Questions");

        builder.HasKey(q => q.Id);

        builder.Property(q => q.Content)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(q => q.ImageUrl)
            .HasMaxLength(1000);

        builder.HasOne(q => q.Classroom)
            .WithMany() // Assuming Classroom doesn't need a collection of all questions
            .HasForeignKey(q => q.ClassroomId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(q => q.Replies)
            .WithOne(r => r.Question)
            .HasForeignKey(r => r.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(q => q.Votes)
            .WithOne(v => v.Question)
            .HasForeignKey(v => v.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasIndex(q => q.ClassroomId);
        builder.HasIndex(q => q.AuthorId);
        builder.HasIndex(q => q.CreatedAt);
    }
}
