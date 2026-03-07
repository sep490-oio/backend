using EFCore.ComplexIndexes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Enums;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
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

            money.Ignore(m => m.Currency);
        });

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

        builder.HasIndex(b => b.BidderId)
            .HasDatabaseName("idx_bids_bidder");
        
        //TODO: consider adding an index on (auction_id, created_at) for efficient retrieval of bid history per auction
        // Complex index on (auction_id, amount) for efficient retrieval of highest bid per auction
        //this index config in migration by add this into the end of up
        //migrationBuilder.CreateIndex(
        // name: "idx_bids_amount",
        // table: "bids",
        // columns: new[] { "auction_id", "amount" },
        // descending: new[] { false, true });
    }
}