using EFCore.ComplexIndexes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OIO.Domain.Context.ReviewContext.Aggregates;

namespace OIO.Infrastructure.Persistence.Configurations.ReviewContext;

internal sealed class SellerReviewConfiguration : IEntityTypeConfiguration<SellerReview>
{
    public void Configure(EntityTypeBuilder<SellerReview> builder)
    {
        builder.ToTable("seller_reviews");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.OrderId)
            .HasColumnName("order_id")
            .IsRequired();

        builder.Property(r => r.AuctionId)
            .HasColumnName("auction_id")
            .IsRequired();

        builder.Property(r => r.ReviewerId)
            .HasColumnName("reviewer_id")
            .IsRequired();

        builder.Property(r => r.SellerId)
            .HasColumnName("seller_id")
            .IsRequired();

        builder.ComplexProperty(r => r.OverallRating, rating =>
        {
            rating.Property(x => x.Value)
                .HasColumnName("overall_rating")
                .IsRequired()
                .HasComplexIndex(indexName: "idx_seller_reviews_rating");
        });

        builder.ComplexProperty(r => r.CommunicationRating, rating =>
        {
            rating.Property(x => x.Value)
                .HasColumnName("communication_rating");
        });

        builder.ComplexProperty(r => r.ShippingSpeedRating, rating =>
        {
            rating.Property(x => x.Value)
                .HasColumnName("shipping_speed_rating");
        });

        builder.ComplexProperty(r => r.ItemAccuracyRating, rating =>
        {
            rating.Property(x => x.Value)
                .HasColumnName("item_accuracy_rating");
        });

        builder.Property(r => r.Title)
            .HasColumnName("title")
            .HasMaxLength(200);

        builder.Property(r => r.Comment)
            .HasColumnName("comment");

        builder.Property(r => r.IsVerifiedPurchase)
            .HasColumnName("is_verified_purchase")
            .HasDefaultValue(true);

        builder.ComplexProperty(r => r.Status, statusBuilder =>
        {
            statusBuilder.Property(s => s.Id)
                .HasColumnName("status")
                .HasMaxLength(20)
                .HasComplexIndex(indexName: "idx_seller_reviews_status");
        });

        builder.Property(r => r.ModerationReason)
            .HasColumnName("moderation_reason");

        builder.Property(r => r.SellerResponse)
            .HasColumnName("seller_response");

        builder.Property(r => r.SellerRespondedAt)
            .HasColumnName("seller_responded_at");

        builder.Property(r => r.HelpfulCount)
            .HasColumnName("helpful_count")
            .HasDefaultValue(0);

        builder.Property(r => r.NotHelpfulCount)
            .HasColumnName("not_helpful_count")
            .HasDefaultValue(0);

        builder.Property(r => r.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.Property(r => r.ModifiedAt)
            .HasColumnName("modified_at");

        // Navigation
        builder.HasMany(r => r.Media)
            .WithOne(m => m.Review)
            .HasForeignKey(m => m.ReviewId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(r => r.Votes)
            .WithOne(v => v.Review)
            .HasForeignKey(v => v.ReviewId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(r => r.Reports)
            .WithOne(rp => rp.Review)
            .HasForeignKey(rp => rp.ReviewId)
            .OnDelete(DeleteBehavior.Cascade);

        // Constraints & Indexes
        builder.HasIndex(r => new { r.OrderId, r.ReviewerId })
            .IsUnique()
            .HasDatabaseName("uq_seller_reviews_order_reviewer");

        builder.HasIndex(r => r.SellerId)
            .HasDatabaseName("idx_seller_reviews_seller");

        builder.HasIndex(r => r.ReviewerId)
            .HasDatabaseName("idx_seller_reviews_reviewer");

        builder.HasIndex(r => r.OrderId)
            .HasDatabaseName("idx_seller_reviews_order");
    }
}
