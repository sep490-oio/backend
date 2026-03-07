using EFCore.ComplexIndexes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Enums;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;

namespace OIO.Infrastructure.Persistence.Configurations.AuctionContext;

internal sealed class AuctionConfiguration : IEntityTypeConfiguration<Auction>
{
    public void Configure(EntityTypeBuilder<Auction> builder)
    {
        builder.ToTable("auctions", t =>
        {
            t.HasCheckConstraint("chk_end_after_start", "end_time > start_time");
            t.HasCheckConstraint("chk_reserve_gte_starting", "reserve_price >= starting_price");
            t.HasCheckConstraint("chk_buy_now_gt_starting", "buy_now_price > starting_price");
            t.HasCheckConstraint("chk_current_gte_starting", "current_price >= starting_price");
            t.HasCheckConstraint("chk_positive_bid_increment", "bid_increment > 0");
        });

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .HasColumnName("id")
            .ValueGeneratedNever()
            .HasConversion(x => x.Value, x => AuctionId.From(x));

        builder.Property(a => a.ItemId)
            .HasColumnName("item_id")
            .IsRequired()
            .HasConversion(x => x.Value, x => ItemId.From(x));

        builder.Property(a => a.SellerId)
            .HasColumnName("seller_id")
            .HasConversion(x => x.Value, x => UserId.From(x));

        // ==================== Money Value Objects ====================
        builder.ComplexProperty(a => a.StartingPrice, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("starting_price")
                .HasColumnType("numeric(18,2)")
                .IsRequired();
            money.Ignore(m => m.Currency);
        });

        builder.ComplexProperty(a => a.ReservePrice, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("reserve_price")
                .HasColumnType("numeric(18,2)");
            money.Ignore(m => m.Currency);
        });

        builder.ComplexProperty(a => a.BuyNowPrice, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("buy_now_price")
                .HasColumnType("numeric(18,2)");
            money.Ignore(m => m.Currency);
        });

        builder.ComplexProperty(a => a.CurrentPrice, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("current_price")
                .HasColumnType("numeric(18,2)")
                .IsRequired();
            money.Ignore(m => m.Currency);
        });

        builder.ComplexProperty(a => a.BidIncrement, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("bid_increment")
                .HasColumnType("numeric(18,2)")
                .HasDefaultValue(1.00m)
                .IsRequired();
            money.Ignore(m => m.Currency);
        });

        builder.Property(a => a.Currency)
            .HasColumnName("currency")
            .HasMaxLength(3)
            .HasDefaultValue("VND");

        // ==================== Duration Value Object ====================
        builder.ComplexProperty(a => a.Duration, duration =>
        {
            duration.Property(d => d.StartTime)
                .HasColumnName("start_time")
                .IsRequired()
                .HasComplexIndex(indexName: "idx_auctions_start_time");

            duration.Property(d => d.EndTime)
                .HasColumnName("end_time")
                .IsRequired()
                .HasComplexIndex(indexName: "idx_auctions_end_time");
        });

        builder.Property(a => a.ActualEndTime)
            .HasColumnName("actual_end_time");

        // ==================== Status ====================
        builder.Property(a => a.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .HasDefaultValue(AuctionStatus.Draft)
            .IsRequired()
            .HasConversion(x => x.Id, x => AuctionStatus.FromId(x).GetValueOrThrow());

        builder.Property(a => a.CurrentWinnerId)
            .HasColumnName("winner_id")
            .HasConversion(x => x.HasValue ? (Guid?)x.Value : null , x => x.HasValue ? UserId.From(x.Value) : null);

        // ==================== Settings ====================
        builder.Property(a => a.AutoExtend)
            .HasColumnName("auto_extend")
            .HasDefaultValue(true);

        builder.Property(a => a.ExtensionMinutes)
            .HasColumnName("extension_minutes")
            .HasDefaultValue(5);

        builder.Property(a => a.IsFeatured)
            .HasColumnName("is_featured")
            .HasDefaultValue(false);

        // ==================== Counters ====================
        builder.Property(a => a.ViewCount)
            .HasColumnName("view_count")
            .HasDefaultValue(0);

        builder.Property(a => a.BidCount)
            .HasColumnName("bid_count")
            .HasDefaultValue(0);

        builder.Property(a => a.WatchCount)
            .HasColumnName("watch_count")
            .HasDefaultValue(0);

        // ==================== Audit ====================
        builder.Property(a => a.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.Property(a => a.ModifiedAt)
            .HasColumnName("modified_at");

        // ==================== Relationships ====================
        builder.HasMany(a => a.Bids)
            .WithOne()
            .HasForeignKey(b => b.AuctionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(a => a.AutoBids)
            .WithOne()
            .HasForeignKey(ab => ab.AuctionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(a => a.Watchers)
            .WithOne()
            .HasForeignKey(w => w.AuctionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(a => a.PriceHistories)
            .WithOne()
            .HasForeignKey(ph => ph.AuctionId)
            .OnDelete(DeleteBehavior.Cascade);

        // ==================== Indexes ====================
        builder.HasIndex(a => a.Status)
            .HasDatabaseName("idx_auctions_status");

        // ==================== Ignore ====================
        builder.Ignore(a => a.DomainEvents);
        builder.Ignore(a => a.IsReserveMet);
        builder.Ignore(a => a.HasBuyNow);
    }
}