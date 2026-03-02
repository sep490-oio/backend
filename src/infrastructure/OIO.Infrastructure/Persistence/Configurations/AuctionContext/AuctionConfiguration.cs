using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Enums;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Shared;

namespace OIO.Infrastructure.Persistence.Configurations.Auctions;

public sealed class AuctionConfiguration : IEntityTypeConfiguration<Auction>
{
    public void Configure(EntityTypeBuilder<Auction> builder)
    {
        builder.ToTable("auctions");

        // Primary Key
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => AuctionId.From(value))
            .HasDefaultValueSql("uuidv7()");

        // Foreign Keys & IDs
        builder.Property(x => x.ItemId)
            .HasConversion(id => id.Value, value => ItemId.From(value))
            .HasColumnName("item_id")
            .IsRequired();

        builder.Property(x => x.CategoryId)
            .HasConversion(id => id.Value, value => CategoryId.From(value))
            .HasColumnName("category_id");

        builder.Property(x => x.SellerId).HasColumnName("seller_id");
        builder.Property(x => x.CurrentWinnerId).HasColumnName("winner_id");

        // WinningConditions (Flattened)
        builder.OwnsOne(x => x.Conditions, c =>
        {
            c.Property(p => p.StartingPrice)
                .HasConversion(m => m.Amount, v => new Money(v, Currency.VND))
                .HasColumnName("starting_price")
                .HasPrecision(18, 2);

            c.Property(p => p.ReservePrice)
                .HasConversion(m => m != null ? (decimal?)m.Amount : null, 
                               v => v.HasValue ? new Money(v.Value, Currency.VND) : null)
                .HasColumnName("reserve_price")
                .HasPrecision(18, 2);

            c.Property(p => p.BuyNowPrice)
                .HasConversion(m => m != null ? (decimal?)m.Amount : null, 
                               v => v.HasValue ? new Money(v.Value, Currency.VND) : null)
                .HasColumnName("buy_now_price")
                .HasPrecision(18, 2);
        });

        // CurrentPrice & Currency
        builder.OwnsOne(x => x.CurrentPrice, m =>
        {
            m.Property(p => p.Amount).HasColumnName("current_price").HasPrecision(18, 2);
            m.Property(p => p.Currency)
                .HasConversion(c => c.Code, v => Currency.FromCode(v))
                .HasColumnName("currency")
                .HasMaxLength(3)
                .HasDefaultValue("VND");
        });

        // Bid Increment
        builder.OwnsOne(x => x.Increment, i =>
        {
            i.Property(p => p.Value)
                .HasConversion(m => m.Amount, v => new Money(v, Currency.VND))
                .HasColumnName("bid_increment")
                .HasDefaultValue(1.00m);
        });

        // AuctionPeriod & Extension Logic (Bổ sung dựa trên schema)
        builder.OwnsOne(x => x.Period, p =>
        {
            p.Property(p => p.StartTime).HasColumnName("start_time");
            p.Property(p => p.EndTime).HasColumnName("end_time");
            // Thuộc tính này nếu có trong AuctionPeriod VO của bạn
            // p.Property(p => p.ActualEndTime).HasColumnName("actual_end_time"); 
        });

        // Bổ sung các trường Boolean & Integer từ Schema
        // builder.Property(x => x.AutoExtend).HasColumnName("auto_extend").HasDefaultValue(true);
        // builder.Property(x => x.ExtensionMinutes).HasColumnName("extension_minutes").HasDefaultValue(5);
        // builder.Property(x => x.IsFeatured).HasColumnName("is_featured").HasDefaultValue(false);
        // builder.Property(x => x.ViewCount).HasColumnName("view_count").HasDefaultValue(0);
        builder.Property(x => x.BidCount).HasColumnName("bid_count").HasDefaultValue(0);
        // builder.Property(x => x.WatchCount).HasColumnName("watch_count").HasDefaultValue(0);

        // Status
        builder.Property(x => x.Status)
            .HasConversion(v => v.ToString().ToLower(), v => (AuctionStatus)Enum.Parse(typeof(AuctionStatus), v, true))
            .HasColumnName("status")
            .HasMaxLength(20);

        // Auditing
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property(x => x.ModifiedAt).HasColumnName("modified_at");
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");

        // --- PriceHistories Collection ---
        builder.OwnsMany(x => x.PriceHistories, h =>
        {
            h.ToTable("auction_price_history");
            h.HasKey(x => x.Id);
            h.Property(x => x.Id).HasConversion(id => id.Value, v => AuctionPriceHistoryId.From(v));
            h.WithOwner().HasForeignKey("AuctionId");
            
            h.Property(x => x.AuctionId)
                .HasConversion(id => id.Value, v => AuctionId.From(v))
                .HasColumnName("auction_id");

            h.Property(x => x.BidderId).HasColumnName("bidder_id");
            h.Property(x => x.Amount)
                .HasConversion(m => m.Amount, v => new Money(v, Currency.VND))
                .HasColumnName("price");

            h.Property(x => x.BidId)
                .HasConversion(id => id.HasValue ? id.Value.Value : (Guid?)null,
                               v => v.HasValue ? BidId.From(v.Value) : default(BidId?))
                .HasColumnName("bid_id");

            h.Property(x => x.CreatedAt).HasColumnName("recorded_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // Quan trọng: Chỉ định cách truy cập field cho Collection
        builder.Navigation(x => x.PriceHistories)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Property(x => x.Version).IsRowVersion();
    }
}