using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Aggregates.Items;
using OIO.Domain.Context.AuctionContext.Enums;
using OIO.Domain.Context.AuctionContext.ValueObjects;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Shared;
using System;

namespace OIO.Infrastructure.Persistence.Configurations.AuctionContext;

internal sealed class AuctionConfiguration : IEntityTypeConfiguration<Auction>
{
    private static readonly Currency Vnd = Currency.FromCode("VND");

    public void Configure(EntityTypeBuilder<Auction> builder)
    {
        builder.ToTable("auctions");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .ValueGeneratedNever()
            .HasColumnName("id")
            .HasConversion(id => id.Value, value => AuctionId.From(value));

        builder.Property(a => a.ItemId)
            .HasColumnName("item_id")
            .IsRequired()
            .HasConversion(id => id.Value, value => ItemId.From(value));

        builder.Ignore(a => a.SellerId);

        builder.Property(a => a.CurrentWinnerId)
            .HasColumnName("winner_id")
            .HasConversion(
                new ValueConverter<UserId?, Guid?>(
                    id => id == null ? (Guid?)null : (Guid?)id.Value,
                    v => v == null ? null : UserId.From(v.Value)));

        builder.OwnsOne(a => a.Conditions, cond =>
        {
            cond.Property(c => c.StartingPrice)
                .HasColumnName("starting_price")
                .IsRequired()
                .HasConversion(m => m.Amount, v => new Money(v, Vnd));

            cond.Property(c => c.ReservePrice)
                .HasColumnName("reserve_price")
                .HasConversion(
                    m => m == null ? (decimal?)null : m.Amount,
                    v => v == null ? null : new Money(v.Value, Vnd));

            cond.Property(c => c.BuyNowPrice)
                .HasColumnName("buy_now_price")
                .HasConversion(
                    m => m == null ? (decimal?)null : m.Amount,
                    v => v == null ? null : new Money(v.Value, Vnd));
        });

        builder.Property(a => a.CurrentPrice)
            .HasColumnName("current_price")
            .IsRequired()
            .HasConversion(m => m.Amount, v => new Money(v, Vnd));

        builder.Property<string>("currency")
            .HasColumnName("currency")
            .HasDefaultValue("VND");

        builder.OwnsOne(a => a.Increment, inc =>
        {
            inc.Property(i => i.Value)
                .HasColumnName("bid_increment")
                .IsRequired()
                .HasConversion(m => m.Amount, v => new Money(v, Vnd));
        });

        builder.OwnsOne(a => a.Period, period =>
        {
            period.Property(p => p.StartTime)
                .HasColumnName("start_time")
                .IsRequired();

            period.Property(p => p.EndTime)
                .HasColumnName("end_time")
                .IsRequired();
        });

        builder.Property(a => a.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .IsRequired()
            .HasDefaultValue(AuctionStatus.Draft)
            .HasConversion(
                s => s.ToString().ToLowerInvariant(),
                s => Enum.Parse<AuctionStatus>(s, ignoreCase: true));

        builder.Property(a => a.AutoExtend)
            .HasColumnName("auto_extend")
            .HasDefaultValue(true);

        builder.Property(a => a.ExtensionMinutes)
            .HasColumnName("extension_minutes")
            .HasDefaultValue(5);

        builder.Property(a => a.IsFeatured)
            .HasColumnName("is_featured")
            .HasDefaultValue(false);

        builder.Property(a => a.ViewCount)
            .HasColumnName("view_count")
            .HasDefaultValue(0);

        builder.Property(a => a.BidCount)
            .HasColumnName("bid_count")
            .HasDefaultValue(0);

        builder.Property(a => a.WatchCount)
            .HasColumnName("watch_count")
            .HasDefaultValue(0);

        builder.Property(a => a.ActualEndTime)
            .HasColumnName("actual_end_time");

        builder.Property(a => a.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.Property(a => a.ModifiedAt)
            .HasColumnName("modified_at");

        builder.HasIndex(a => a.Status)
            .HasDatabaseName("idx_auctions_status");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("chk_end_after_start", "end_time > start_time");
            t.HasCheckConstraint("chk_current_gte_starting", "current_price >= starting_price");
            t.HasCheckConstraint("chk_buy_now_gt_starting", "buy_now_price > starting_price");
            t.HasCheckConstraint("chk_reserve_gte_starting", "reserve_price >= starting_price");
            t.HasCheckConstraint("chk_positive_bid_increment", "bid_increment > 0");
            t.HasCheckConstraint(
                "auctions_status_check",
                "status IN ('draft','pending','active','ended','sold','cancelled','failed')");
        });

        builder.HasOne<Item>()
            .WithMany()
            .HasForeignKey(a => a.ItemId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("auctions_item_id_fkey");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(a => a.CurrentWinnerId)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("auctions_winner_id_fkey");

        builder.Ignore(a => a.DomainEvents);
    }
}

internal sealed class AuctionPriceHistoryConfiguration : IEntityTypeConfiguration<AuctionPriceHistory>
{
    private static readonly Currency Vnd = Currency.FromCode("VND");

    public void Configure(EntityTypeBuilder<AuctionPriceHistory> builder)
    {
        builder.ToTable("auction_price_history");

        builder.HasKey(p => p.Id);
        
        builder.Property(p => p.Id)
            .HasColumnName("id")
            .ValueGeneratedNever()
            .HasConversion(id => id.Value, v => AuctionPriceHistoryId.From(v)); 

        builder.Property(p => p.Price)
            .HasColumnName("price")
            .IsRequired()
            .HasConversion(m => m.Amount, v => new Money(v, Vnd));

        builder.Property(p => p.BidId)
            .HasColumnName("bid_id")
            .HasConversion(
                new ValueConverter<BidId?, Guid?>(
                    id => id == null ? (Guid?)null : (Guid?)id.Value,
                    v => v == null ? null : BidId.From(v.Value)));

        builder.Property(p => p.RecordedAt)
            .HasColumnName("recorded_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.HasIndex(p => p.AuctionId)
            .HasDatabaseName("idx_auction_price_history_auction_id");

        builder.HasOne<Auction>()
               .WithMany(a => a.PriceHistories)
               .HasForeignKey(p => p.AuctionId)
               .HasConstraintName("auction_price_history_auction_id_fkey");
    }
}

internal sealed class AuctionDepositConfiguration : IEntityTypeConfiguration<AuctionDeposit>
{
    private static readonly Currency Vnd = Currency.FromCode("VND");

    public void Configure(EntityTypeBuilder<AuctionDeposit> builder)
    {
        builder.ToTable("auction_deposits");

        builder.HasKey(d => d.Id);
        
        builder.Property(d => d.Id)
            .HasColumnName("id")
            .ValueGeneratedNever()
            .HasConversion(id => id.Value, v => AuctionDepositId.From(v));

        builder.Property(d => d.UserId)
            .HasColumnName("user_id")
            .IsRequired()
            .HasConversion(id => id.Value, v => UserId.From(v));

        builder.Property(d => d.Amount)
            .HasColumnName("amount")
            .IsRequired()
            .HasConversion(m => m.Amount, v => new Money(v, Vnd));

        builder.Property(d => d.TransactionId)
            .HasColumnName("transaction_id")
            .HasConversion(
                new ValueConverter<TransactionId?, Guid?>(
                    id => id == null ? (Guid?)null : (Guid?)id.Value,
                    v => v == null ? null : TransactionId.From(v.Value)));

        builder.Property(d => d.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .IsRequired()
            .HasDefaultValue(DepositStatus.Held)
            .HasConversion(
                s => s.ToString().ToLowerInvariant(),
                s => Enum.Parse<DepositStatus>(s, ignoreCase: true));

        builder.Property(d => d.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.Property(d => d.ReleasedAt)
            .HasColumnName("released_at");

        builder.HasIndex(d => new { d.AuctionId, d.UserId })
            .IsUnique()
            .HasDatabaseName("auction_deposits_auction_id_user_id_key");

        builder.ToTable(t => t.HasCheckConstraint(
            "auction_deposits_status_check",
            "status IN ('held','returned','forfeited','converted_to_payment')"));

        builder.HasOne<Auction>()
               .WithMany(a => a.Deposits)
               .HasForeignKey(d => d.AuctionId)
               .HasConstraintName("auction_deposits_auction_id_fkey");
    }
}

internal sealed class AuctionAutoBidConfiguration : IEntityTypeConfiguration<AuctionAutoBid>
{
    private static readonly Currency Vnd = Currency.FromCode("VND");

    public void Configure(EntityTypeBuilder<AuctionAutoBid> builder)
    {
        builder.ToTable("auction_auto_bids");

        builder.HasKey(b => b.Id);
        
        builder.Property(b => b.Id)
            .HasColumnName("id")
            .ValueGeneratedNever()
            .HasConversion(id => id.Value, v => AuctionAutoBidId.From(v));

        builder.Property(b => b.BidderId)
            .HasColumnName("bidder_id")
            .IsRequired()
            .HasConversion(id => id.Value, v => UserId.From(v));

        builder.Property(b => b.IsEnabled)
            .HasColumnName("is_enabled")
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(b => b.MaxAmount)
            .HasColumnName("max_amount")
            .IsRequired()
            .HasConversion(m => m.Amount, v => new Money(v, Vnd));

        builder.Property(b => b.CurrentAmount)
            .HasColumnName("current_amount")
            .IsRequired()
            .HasConversion(m => m.Amount, v => new Money(v, Vnd));

        builder.Property(b => b.IncrementAmount)
            .HasColumnName("increment_amount")
            .HasConversion(
                m => m == null ? (decimal?)null : m.Amount,
                v => v == null ? null : new Money(v.Value, Vnd));

        builder.Property(b => b.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .IsRequired()
            .HasDefaultValue(AutoBidStatus.Active)
            .HasConversion(
                s => s.ToString().ToLowerInvariant(),
                s => Enum.Parse<AutoBidStatus>(s, ignoreCase: true));

        builder.Property(b => b.TotalAutoBids)
            .HasColumnName("total_auto_bids")
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(b => b.LastAutoBidAt)
            .HasColumnName("last_auto_bid_at");

        builder.Property(b => b.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.Property(b => b.ModifiedAt)
            .HasColumnName("modified_at");

        builder.HasIndex(b => new { b.AuctionId, b.BidderId })
            .IsUnique()
            .HasDatabaseName("auction_auto_bids_auction_id_bidder_id_key");

        builder.HasIndex(b => b.AuctionId)
            .HasDatabaseName("idx_auction_auto_bids_auction_id");

        builder.HasIndex(b => new { b.AuctionId, b.Status })
            .HasDatabaseName("idx_auction_auto_bids_auction_id_status");

        builder.HasIndex(b => b.BidderId)
            .HasDatabaseName("idx_auction_auto_bids_bidder_id");

        builder.ToTable(t => t.HasCheckConstraint(
            "auction_auto_bids_status_check",
            "status IN ('active','paused','exhausted','won','outbid')"));

        builder.HasOne<Auction>()
               .WithMany(a => a.AutoBids)
               .HasForeignKey(b => b.AuctionId)
               .HasConstraintName("auction_auto_bids_auction_id_fkey");
    }
}

internal sealed class AuctionWatcherConfiguration : IEntityTypeConfiguration<AuctionWatcher>
{
    public void Configure(EntityTypeBuilder<AuctionWatcher> builder)
    {
        builder.ToTable("auction_watchers");

        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever()
            .HasConversion(id => id.Value, v => AuctionWatcherId.From(v));

        builder.Property(x => x.UserId)
            .HasColumnName("user_id")
            .IsRequired()
            .HasConversion(id => id.Value, v => UserId.From(v));

        builder.Property(x => x.NotifyOnBid)
            .HasColumnName("notify_on_bid")
            .HasDefaultValue(true);

        builder.Property(x => x.NotifyOnEnd)
            .HasColumnName("notify_on_end")
            .HasDefaultValue(true);

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.HasIndex(x => new { x.AuctionId, x.UserId })
            .IsUnique()
            .HasDatabaseName("auction_watchers_auction_id_user_id_key");

        builder.HasOne<Auction>()
               .WithMany(a => a.Watchers)
               .HasForeignKey(w => w.AuctionId)
               .HasConstraintName("auction_watchers_auction_id_fkey");
    }
}