using EFCore.ComplexIndexes;
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
        builder.ComplexProperty(ab => ab.Budget, budget =>
        {
            budget.Property(b => b.MaxAmount)
                .HasColumnName("max_amount")
                .HasPrecision(18, 2)
                .IsRequired();

            budget.Property(b => b.CurrentAmount)
                .HasColumnName("current_amount")
                .HasPrecision(18, 2)
                .IsRequired();

            budget.Property(b => b.IncrementAmount)
                .HasColumnName("increment_amount")
                .HasPrecision(18, 2);

            budget.Property(b => b.ReservedAmount)
                .HasColumnName("reserved_amount")
                .HasPrecision(18, 2)
                .IsRequired();

            budget.ComplexProperty(b => b.Currency, currency =>
            {
                currency.Property(c => c.Id)
                    .HasColumnName("currency")
                    .HasMaxLength(3)
                    .IsRequired(); 
            });
        });
        
        builder.ComplexProperty(x => x.Status, statusBuilder =>
        {
            statusBuilder.Property(a => a.Id)
                .HasColumnName("status")
                .HasMaxLength(20)
                .HasDefaultValue(AutoBidStatus.Active.Id)
                .IsRequired();
        }).HasComplexCompositeIndex(ab => new { ab.AuctionId, ab.Status.Id }, indexName: "idx_auction_auto_bids_auction_id_status", filter: "status = 'active'");


        builder.Property(ab => ab.TotalAutoBids)
            .HasColumnName("total_auto_bids")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(ab => ab.LastAutoBidAt)
            .HasColumnName("last_auto_bid_at");

        builder.Property(ab => ab.StopReason)
            .HasColumnName("stop_reason");
        
        builder.Property(ab => ab.StoppedAt)
            .HasColumnName("stopped_at");

        builder.Property(ab => ab.LastValidationAt)
            .HasColumnName("last_validation_at");

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
        
        builder.HasIndex(ab => ab.LastValidationAt)
            .HasDatabaseName("idx_auction_auto_bids_last_validation_at");
        
        builder.HasIndex(ab => ab.StoppedAt)
            .HasDatabaseName("idx_auction_auto_bids_stopped_at")
            .HasFilter("stopped_at IS NOT NULL");
    }
}