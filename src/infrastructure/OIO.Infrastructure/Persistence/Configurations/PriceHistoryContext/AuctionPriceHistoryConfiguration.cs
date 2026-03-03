using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Shared;

namespace OIO.Infrastructure.Persistence.Configurations.PriceHistoryContext;

public sealed class AuctionPriceHistoryConfiguration : IEntityTypeConfiguration<AuctionPriceHistory>
{
    public void Configure(EntityTypeBuilder<AuctionPriceHistory> builder)
    {
        // --- Table Mapping ---
        builder.ToTable("auction_price_history");

        // --- Primary Key ---
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, v => AuctionPriceHistoryId.From(v))
            .HasColumnName("id")
            .HasDefaultValueSql("uuidv7()");

        // --- Foreign Key: AuctionId ---
        builder.Property(x => x.AuctionId)
            .HasConversion(id => id.Value, v => AuctionId.From(v))
            .HasColumnName("auction_id")
            .IsRequired();

        // --- Foreign Key: BidId (Nullable Value Object) ---
        // Fix lỗi "Cannot convert expression type to return type 'System.Guid?'"
        builder.Property(x => x.BidId)
            .HasConversion<Guid?>(
                id => id != null ? (Guid?)id.Value : null, 
                v => v.HasValue ? BidId.From(v.Value) : null
            )
            .HasColumnName("bid_id");

        // --- Price (Money VO) ---
        builder.OwnsOne(x => x.Price, m =>
        {
            m.Property(p => p.Amount)
                .HasColumnName("price")
                .HasPrecision(18, 2)
                .IsRequired();
            
            // Script DB không có cột currency cho bảng này nên ta Ignore
            m.Ignore(p => p.Currency);
        });

        // --- Recorded At ---
        builder.Property(x => x.RecordedAt)
            .HasColumnName("recorded_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        // --- Relationships (Khớp với DataContext) ---
        
        // N-1 với Auction
        builder.HasOne<Auction>()
            .WithMany(a => a.PriceHistories)
            .HasForeignKey(x => x.AuctionId)
            .OnDelete(DeleteBehavior.Cascade);

        // N-1 với Bid (Nếu có navigation property)
        // builder.HasOne<Bid>()
        //    .WithMany()
        //    .HasForeignKey(x => x.BidId)
        //    .OnDelete(DeleteBehavior.SetNull);
    }
}