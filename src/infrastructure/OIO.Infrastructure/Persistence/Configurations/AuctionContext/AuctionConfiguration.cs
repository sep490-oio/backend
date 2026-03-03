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

        // --- Primary Key & Basic Properties ---
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, v => AuctionId.From(v))
            .HasColumnName("id")
            .HasDefaultValueSql("uuidv7()");

        builder.Property(x => x.ItemId)
            .HasConversion(id => id.Value, v => ItemId.From(v))
            .HasColumnName("item_id");

        builder.Property(x => x.CurrentWinnerId).HasColumnName("winner_id");

        // --- Value Objects Mapping (Flattened) ---
        builder.OwnsOne(x => x.Conditions, c =>
        {
            c.Property(p => p.StartingPrice).HasConversion(m => m.Amount, v => new Money(v, Currency.VND)).HasColumnName("starting_price").HasPrecision(18, 2);
            c.Property(p => p.ReservePrice).HasConversion(m => m != null ? (decimal?)m.Amount : null, v => v.HasValue ? new Money(v.Value, Currency.VND) : null).HasColumnName("reserve_price").HasPrecision(18, 2);
            c.Property(p => p.BuyNowPrice).HasConversion(m => m != null ? (decimal?)m.Amount : null, v => v.HasValue ? new Money(v.Value, Currency.VND) : null).HasColumnName("buy_now_price").HasPrecision(18, 2);
        });

        builder.OwnsOne(x => x.CurrentPrice, m =>
        {
            m.Property(p => p.Amount).HasColumnName("current_price").HasPrecision(18, 2);
            m.Property(p => p.Currency).HasConversion(c => c.Code, v => Currency.FromCode(v)).HasColumnName("currency").HasDefaultValue("VND").HasMaxLength(3);
        });

        builder.OwnsOne(x => x.Increment, i =>
            i.Property(p => p.Value).HasConversion(m => m.Amount, v => new Money(v, Currency.VND)).HasColumnName("bid_increment").HasPrecision(18, 2).HasDefaultValue(1.00m));

        builder.OwnsOne(x => x.Period, p =>
        {
            p.Property(p => p.StartTime).HasColumnName("start_time");
            p.Property(p => p.EndTime).HasColumnName("end_time");
        });

        // --- Status & Meta Data ---
        builder.Property(x => x.Status)
            .HasConversion(v => v.ToString().ToLower(), v => (AuctionStatus)Enum.Parse(typeof(AuctionStatus), v, true))
            .HasColumnName("status").HasDefaultValue("draft");

        builder.Property(x => x.AutoExtend).HasColumnName("auto_extend").HasDefaultValue(true);
        builder.Property(x => x.ExtensionMinutes).HasColumnName("extension_minutes").HasDefaultValue(5);
        builder.Property(x => x.IsFeatured).HasColumnName("is_featured").HasDefaultValue(false);
        builder.Property(x => x.ViewCount).HasColumnName("view_count").HasDefaultValue(0);
        builder.Property(x => x.BidCount).HasColumnName("bid_count").HasDefaultValue(0);
        builder.Property(x => x.WatchCount).HasColumnName("watch_count").HasDefaultValue(0);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property(x => x.ModifiedAt).HasColumnName("modified_at");

        // --- Indexes ---
        builder.HasIndex(x => new { x.Status, x.Period.EndTime }).HasDatabaseName("idx_auctions_active");
        builder.HasIndex(x => x.Status).HasDatabaseName("idx_auctions_status");

        // ==========================================
        // OWNED COLLECTIONS (Navigation & Tables)
        // ==========================================

        // 1. Price Histories
        builder.OwnsMany(x => x.PriceHistories, h =>
        {
            h.ToTable("auction_price_history");
            h.HasKey(p => p.Id);
            h.Property(p => p.Id).HasConversion(id => id.Value, v => AuctionPriceHistoryId.From(v)).HasColumnName("id");
            h.WithOwner().HasForeignKey("AuctionId");
            h.Property<AuctionId>("AuctionId").HasConversion(id => id.Value, v => AuctionId.From(v)).HasColumnName("auction_id");
            h.OwnsOne(p => p.Price, m => m.Property(p => p.Amount).HasColumnName("price").HasPrecision(18, 2).IsRequired());
            h.Property(p => p.BidId).HasConversion<Guid?>(id => id != null ? (Guid?)id.Value : null, v => v.HasValue ? BidId.From(v.Value) : null).HasColumnName("bid_id");
            h.Property(p => p.RecordedAt).HasColumnName("recorded_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // 2. Deposits
        builder.OwnsMany(x => x.Deposits, d =>
        {
            d.ToTable("auction_deposits");
            d.HasKey(p => p.Id);
            d.Property(p => p.Id).HasConversion(id => id.Value, v => AuctionDepositId.From(v)).HasColumnName("id");
            d.WithOwner().HasForeignKey("AuctionId");
            d.Property<AuctionId>("AuctionId").HasConversion(id => id.Value, v => AuctionId.From(v)).HasColumnName("auction_id");
            d.Property(p => p.UserId).HasColumnName("user_id");
            d.OwnsOne(p => p.Amount, m => m.Property(p => p.Amount).HasColumnName("amount").HasPrecision(18, 2));
            d.Property(p => p.Status).HasConversion(v => v.ToString().ToLower(), v => (DepositStatus)Enum.Parse(typeof(DepositStatus), v, true)).HasColumnName("status");
            d.Property(p => p.TransactionId).HasColumnName("transaction_id");
            d.Property(p => p.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            d.Property(p => p.ReleasedAt).HasColumnName("released_at");
        });

        // 3. Auto Bids
        builder.OwnsMany(x => x.AutoBids, a =>
        {
            a.ToTable("auction_auto_bids");
            a.HasKey(p => p.Id);
            a.Property(p => p.Id).HasConversion(id => id.Value, v => AuctionAutoBidId.From(v)).HasColumnName("id");
            a.WithOwner().HasForeignKey("AuctionId");
            a.Property<AuctionId>("AuctionId").HasConversion(id => id.Value, v => AuctionId.From(v)).HasColumnName("auction_id");
            a.Property(p => p.BidderId).HasColumnName("bidder_id");
            a.Property(p => p.IsEnabled).HasColumnName("is_enabled");
            a.OwnsOne(p => p.MaxAmount, m => m.Property(p => p.Amount).HasColumnName("max_amount").HasPrecision(18, 2));
            a.OwnsOne(p => p.CurrentAmount, m => m.Property(p => p.Amount).HasColumnName("current_amount").HasPrecision(18, 2));
            a.OwnsOne(p => p.IncrementAmount, m => m.Property(p => p.Amount).HasColumnName("increment_amount").HasPrecision(18, 2));
            a.Property(p => p.Status).HasConversion(v => v.ToString().ToLower(), v => (AutoBidStatus)Enum.Parse(typeof(AutoBidStatus), v, true)).HasColumnName("status");
            a.Property(p => p.CreatedAt).HasColumnName("created_at");
            a.Property(p => p.ModifiedAt).HasColumnName("modified_at");
        });

        // 4. Watchers
        builder.OwnsMany(x => x.Watchers, w =>
        {
            w.ToTable("auction_watchers");
            w.HasKey(p => p.Id);
            w.Property(p => p.Id).HasConversion(id => id.Value, v => AuctionWatcherId.From(v)).HasColumnName("id");
            w.WithOwner().HasForeignKey("AuctionId");
            w.Property<AuctionId>("AuctionId").HasConversion(id => id.Value, v => AuctionId.From(v)).HasColumnName("auction_id");
            w.Property(p => p.UserId).HasColumnName("user_id");
            w.Property(p => p.NotifyOnBid).HasColumnName("notify_on_bid").HasDefaultValue(true);
            w.Property(p => p.NotifyOnEnd).HasColumnName("notify_on_end").HasDefaultValue(true);
            w.Property(p => p.CreatedAt).HasColumnName("created_at");
        });

        // Cấu hình Field Access cho các private List
        builder.Navigation(x => x.PriceHistories).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(x => x.Deposits).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(x => x.AutoBids).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(x => x.Watchers).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}