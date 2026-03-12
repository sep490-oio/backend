using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OIO.Domain.Context.ReviewContext.Aggregates;

namespace OIO.Infrastructure.Persistence.Configurations.ReviewContext;

internal sealed class BuyerReviewConfiguration : IEntityTypeConfiguration<BuyerReview>
{
    public void Configure(EntityTypeBuilder<BuyerReview> builder)
    {
        builder.ToTable("buyer_reviews");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.OrderId)
            .HasColumnName("order_id")
            .IsRequired();

        builder.Property(r => r.ReviewerId)
            .HasColumnName("reviewer_id")
            .IsRequired();

        builder.Property(r => r.BuyerId)
            .HasColumnName("buyer_id")
            .IsRequired();

        builder.ComplexProperty(r => r.OverallRating, rating =>
        {
            rating.Property(x => x.Value)
                .HasColumnName("overall_rating")
                .IsRequired();
        });

        builder.ComplexProperty(r => r.PaymentSpeedRating, rating =>
        {
            rating.Property(x => x.Value)
                .HasColumnName("payment_speed_rating");
        });

        builder.ComplexProperty(r => r.CommunicationRating, rating =>
        {
            rating.Property(x => x.Value)
                .HasColumnName("communication_rating");
        });

        builder.Property(r => r.Comment)
            .HasColumnName("comment");

        builder.ComplexProperty(r => r.Status, statusBuilder =>
        {
            statusBuilder.Property(s => s.Id)
                .HasColumnName("status")
                .HasMaxLength(20);
        });

        builder.Property(r => r.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        // Constraints & Indexes
        builder.HasIndex(r => new { r.OrderId, r.ReviewerId })
            .IsUnique()
            .HasDatabaseName("uq_buyer_reviews_order_reviewer");

        builder.HasIndex(r => r.BuyerId)
            .HasDatabaseName("idx_buyer_reviews_buyer");
    }
}
