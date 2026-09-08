using Draya.Domain.Classrooms;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Draya.Infrastructure.Persistence.Configurations;

public class QuestionVoteConfiguration : IEntityTypeConfiguration<QuestionVote>
{
    public void Configure(EntityTypeBuilder<QuestionVote> builder)
    {
        builder.ToTable("QuestionVotes");

        // Composite primary key ensures a user can only vote once per question
        builder.HasKey(v => new { v.QuestionId, v.UserId });

        builder.HasIndex(v => v.UserId);
    }
}
