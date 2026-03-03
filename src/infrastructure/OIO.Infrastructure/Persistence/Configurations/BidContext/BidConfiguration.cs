using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OIO.Domain.Context.AuctionContext.Aggregates.Bids;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Shared;
using OIO.Domain.Context.AuctionContext.Enums;
namespace OIO.Infrastructure.Persistence.Configurations.BidContext;

public sealed class BidConfiguration : IEntityTypeConfiguration<Bid>
{
    public void Configure(EntityTypeBuilder<Bid> builder)
    {
        builder.ToTable("bids");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => BidId.From(value))
            .HasColumnName("id")
            .HasDefaultValueSql("uuidv7()");

        builder.Property(x => x.AuctionId)
            .HasConversion(id => id.Value, value => AuctionId.From(value))
            .HasColumnName("auction_id")
            .IsRequired();

        builder.Property(x => x.BidderId).HasColumnName("bidder_id").IsRequired();

        builder.OwnsOne(x => x.Amount, m =>
        {
            m.Property(p => p.Amount).HasColumnName("amount").HasPrecision(18, 2);
            m.Ignore(p => p.Currency); // Vì script DB không có cột currency ở bảng bids
        });

        builder.Property(x => x.IsAutoBid).HasColumnName("is_auto_bid");
        builder.Property(x => x.AutoBidId).HasColumnName("auto_bid_id");
        
        builder.Property(x => x.Status)
            .HasConversion(v => v.ToString().ToLower(), v => (BidStatus)Enum.Parse(typeof(BidStatus), v, true))
            .HasColumnName("status")
            .HasDefaultValue("active");

        builder.Property(x => x.IpAddress).HasColumnName("ip_address");

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // --- Indexes từ Script DB ---
        builder.HasIndex(x => new { x.AuctionId, x.Amount }).HasDatabaseName("idx_bids_amount");
        builder.HasIndex(x => x.AuctionId).HasDatabaseName("idx_bids_auction");
        builder.HasIndex(x => x.BidderId).HasDatabaseName("idx_bids_bidder");
    }
}