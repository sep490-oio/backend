using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OIO.Domain.Context.ReviewContext.Aggregates;

namespace OIO.Infrastructure.Persistence.Configurations.ReviewContext;

internal sealed class SellerRatingSummaryConfiguration : IEntityTypeConfiguration<SellerRatingSummary>
{
    public void Configure(EntityTypeBuilder<SellerRatingSummary> builder)
    {
        builder.ToTable("seller_rating_summary");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.SellerId)
            .HasColumnName("seller_id")
            .IsRequired();

        builder.Property(s => s.TotalReviews)
            .HasColumnName("total_reviews")
            .HasDefaultValue(0);

        builder.Property(s => s.AverageRating)
            .HasColumnName("average_rating")
            .HasColumnType("numeric(3,2)")
            .HasDefaultValue(0m);

        builder.Property(s => s.Rating5Count)
            .HasColumnName("rating_5_count")
            .HasDefaultValue(0);

        builder.Property(s => s.Rating4Count)
            .HasColumnName("rating_4_count")
            .HasDefaultValue(0);

        builder.Property(s => s.Rating3Count)
            .HasColumnName("rating_3_count")
            .HasDefaultValue(0);

        builder.Property(s => s.Rating2Count)
            .HasColumnName("rating_2_count")
            .HasDefaultValue(0);

        builder.Property(s => s.Rating1Count)
            .HasColumnName("rating_1_count")
            .HasDefaultValue(0);

        builder.Property(s => s.AvgCommunication)
            .HasColumnName("avg_communication")
            .HasColumnType("numeric(3,2)")
            .HasDefaultValue(0m);

        builder.Property(s => s.AvgShippingSpeed)
            .HasColumnName("avg_shipping_speed")
            .HasColumnType("numeric(3,2)")
            .HasDefaultValue(0m);

        builder.Property(s => s.AvgItemAccuracy)
            .HasColumnName("avg_item_accuracy")
            .HasColumnType("numeric(3,2)")
            .HasDefaultValue(0m);

        builder.Property(s => s.ResponseCount)
            .HasColumnName("response_count")
            .HasDefaultValue(0);

        builder.Property(s => s.LastUpdatedAt)
            .HasColumnName("last_updated_at");

        // Constraints
        builder.HasIndex(s => s.SellerId)
            .IsUnique()
            .HasDatabaseName("uq_seller_rating_summary_seller");
    }
}
