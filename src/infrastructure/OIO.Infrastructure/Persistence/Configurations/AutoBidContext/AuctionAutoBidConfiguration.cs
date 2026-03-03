using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Enums;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Shared;

namespace OIO.Infrastructure.Persistence.Configurations.AutoBidContext;

public sealed class AuctionAutoBidConfiguration : IEntityTypeConfiguration<AuctionAutoBid>
{
    public void Configure(EntityTypeBuilder<AuctionAutoBid> builder)
    {
        builder.ToTable("auction_auto_bids");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, v => AuctionAutoBidId.From(v))
            .HasColumnName("id")
            .HasDefaultValueSql("uuidv7()");

        builder.Property(x => x.AuctionId)
            .HasConversion(id => id.Value, v => AuctionId.From(v))
            .HasColumnName("auction_id");

        builder.Property(x => x.BidderId).HasColumnName("bidder_id");
        builder.Property(x => x.IsEnabled).HasColumnName("is_enabled").HasDefaultValue(true);
        builder.Property(x => x.TotalAutoBids).HasColumnName("total_auto_bids").HasDefaultValue(0);

        // Money Mappings
        builder.OwnsOne(x => x.MaxAmount, m => 
            m.Property(p => p.Amount).HasColumnName("max_amount").HasPrecision(18, 2));

        builder.OwnsOne(x => x.CurrentAmount, m => 
            m.Property(p => p.Amount).HasColumnName("current_amount").HasPrecision(18, 2));

        builder.OwnsOne(x => x.IncrementAmount, m => 
            m.Property(p => p.Amount).HasColumnName("increment_amount").HasPrecision(18, 2));

        // Enum Status Mapping (Lowercase)
        builder.Property(x => x.Status)
            .HasConversion(
                v => v.ToString().ToLower(), 
                v => (AutoBidStatus)Enum.Parse(typeof(AutoBidStatus), v, true))
            .HasColumnName("status")
            .HasDefaultValue("active");

        builder.Property(x => x.LastAutoBidAt).HasColumnName("last_auto_bid_at");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property(x => x.ModifiedAt).HasColumnName("modified_at");

        // --- Indexes & Unique Constraints ---
        builder.HasIndex(x => new { x.AuctionId, x.BidderId })
            .IsUnique()
            .HasDatabaseName("auction_auto_bids_auction_id_bidder_id_key");

        builder.HasIndex(x => x.AuctionId).HasDatabaseName("idx_auction_auto_bids_auction_id");
        builder.HasIndex(x => x.BidderId).HasDatabaseName("idx_auction_auto_bids_bidder_id");
        builder.HasIndex(x => new { x.AuctionId, x.Status }).HasDatabaseName("idx_auction_auto_bids_auction_id_status");
    }
}