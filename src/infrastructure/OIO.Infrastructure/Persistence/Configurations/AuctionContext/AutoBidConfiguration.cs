using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Enums;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;

namespace OIO.Infrastructure.Persistence.Configurations.AuctionContext;

internal sealed class AutoBidConfiguration : IEntityTypeConfiguration<AutoBid>
{
    public void Configure(EntityTypeBuilder<AutoBid> builder)
    {
        builder.ToTable("auction_auto_bids");

        builder.HasKey(ab => ab.Id);

        builder.Property(ab => ab.Id)
            .HasColumnName("id")
            .ValueGeneratedNever()
            .HasConversion(x => x.Value, x => AutoBidId.From(x));

        builder.Property(ab => ab.AuctionId)
            .HasColumnName("auction_id")
            .IsRequired()
            .HasConversion(x => x.Value, x => AuctionId.From(x));

        builder.Property(ab => ab.BidderId)
            .HasColumnName("bidder_id")
            .IsRequired()
            .HasConversion(x => x.Value, x => UserId.From(x));

        builder.Property(ab => ab.IsEnabled)
            .HasColumnName("is_enabled")
            .HasDefaultValue(true)
            .IsRequired();

        // Money value objects mapped as owned types
        builder.ComplexProperty(ab => ab.MaxAmount, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("max_amount")
                .HasColumnType("numeric(18,2)")
                .IsRequired();

            money.Ignore(m => m.Currency);
        });

        builder.ComplexProperty(ab => ab.CurrentAmount, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("current_amount")
                .HasColumnType("numeric(18,2)")
                .IsRequired();

            money.Ignore(m => m.Currency);
        });

        builder.ComplexProperty(ab => ab.IncrementAmount, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("increment_amount")
                .HasColumnType("numeric(18,2)");

            money.Ignore(m => m.Currency);
        });

        builder.Property(ab => ab.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .HasDefaultValue(AutoBidStatus.Active)
            .IsRequired()
            .HasConversion(x => x.Id, x => AutoBidStatus.FromId(x).GetValueOrThrow());

        builder.Property(ab => ab.TotalAutoBids)
            .HasColumnName("total_auto_bids")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(ab => ab.LastAutoBidAt)
            .HasColumnName("last_auto_bid_at");

        builder.Property(ab => ab.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.Property(ab => ab.ModifiedAt)
            .HasColumnName("modified_at");

        // UNIQUE(auction_id, bidder_id)
        builder.HasIndex(ab => new { ab.AuctionId, ab.BidderId })
            .IsUnique();

        // Indexes
        builder.HasIndex(ab => ab.AuctionId)
            .HasDatabaseName("idx_auction_auto_bids_auction_id");

        builder.HasIndex(ab => ab.BidderId)
            .HasDatabaseName("idx_auction_auto_bids_bidder_id");

        builder.HasIndex(ab => new { ab.AuctionId, ab.Status })
            .HasDatabaseName("idx_auction_auto_bids_auction_id_status")
            .HasFilter("status = 'active'");
    }
}