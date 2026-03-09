using EFCore.ComplexIndexes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Enums;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.Shared.ValueObjects;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;

namespace OIO.Infrastructure.Persistence.Configurations.AuctionContext;

internal sealed class BidConfiguration : IEntityTypeConfiguration<Bid>
{
    public void Configure(EntityTypeBuilder<Bid> builder)
    {
        builder.ToTable("bids");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.Id)
            .HasColumnName("id")
            .ValueGeneratedNever()
            .HasConversion(x => x.Value, x => BidId.From(x));

        builder.Property(b => b.AuctionId)
            .HasColumnName("auction_id")
            .IsRequired()
            .HasConversion(x => x.Value, x => AuctionId.From(x));

        builder.Property(b => b.BidderId)
            .HasColumnName("bidder_id")
            .IsRequired()
            .HasConversion(x => x.Value, x => UserId.From(x));

        builder.ComplexProperty(b => b.Amount, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("amount")
                .HasColumnType("numeric(18,2)")
                .IsRequired();

            money.ComplexProperty(m => m.Currency, currencyBuilder =>
            {
                currencyBuilder.Property(x => x.Id)
                    .HasColumnName("currency")
                    .HasMaxLength(3)
                    .IsRequired();
            });

        }).HasComplexCompositeIndex(x => new { x.AuctionId, x.Amount.Amount });

        // is_auto_bid is GENERATED ALWAYS in DB — mark as computed
        builder.Property(b => b.IsAutoBid)
            .HasColumnName("is_auto_bid")
            .HasComputedColumnSql("(auto_bid_id IS NOT NULL)", stored: true);

        builder.Property(b => b.AutoBidId)
            .HasColumnName("auto_bid_id");

        builder.Property(b => b.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .HasDefaultValue(BidStatus.Active)
            .IsRequired()
            .HasConversion(x => x.Id, x => BidStatus.FromId(x).GetValueOrThrow());

        builder.Property(b => b.IpAddress)
            .HasColumnName("ip_address")
            .HasColumnType("inet");

        builder.Property(b => b.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        // Foreign key to auto_bid
        builder.HasOne<AutoBid>()
            .WithMany()
            .HasForeignKey(b => b.AutoBidId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes
        builder.HasIndex(b => b.AuctionId)
            .HasDatabaseName("idx_bids_auction");
        
        builder.HasIndex(b => new { b.AuctionId, b.CreatedAt})
            .HasDatabaseName("idx_bids_auction_created_at");

        builder.HasIndex(b => b.BidderId)
            .HasDatabaseName("idx_bids_bidder");
    }
}