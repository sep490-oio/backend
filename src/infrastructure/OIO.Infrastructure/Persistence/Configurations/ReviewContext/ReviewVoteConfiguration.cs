using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OIO.Domain.Context.ReviewContext.Aggregates;

namespace OIO.Infrastructure.Persistence.Configurations.ReviewContext;

internal sealed class ReviewVoteConfiguration : IEntityTypeConfiguration<ReviewVote>
{
    public void Configure(EntityTypeBuilder<ReviewVote> builder)
    {
        builder.ToTable("review_votes");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.ReviewId)
            .HasColumnName("review_id")
            .IsRequired();

        builder.Property(v => v.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(v => v.IsHelpful)
            .HasColumnName("is_helpful")
            .IsRequired();

        builder.Property(v => v.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        // Constraints
        builder.HasIndex(v => new { v.ReviewId, v.UserId })
            .IsUnique()
            .HasDatabaseName("uq_review_votes_review_user");
    }
}
