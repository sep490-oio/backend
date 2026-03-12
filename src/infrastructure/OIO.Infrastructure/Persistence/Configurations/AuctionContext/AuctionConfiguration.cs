using EFCore.ComplexIndexes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Enums;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.Shared.ValueObjects;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using ItemId = OIO.Domain.Context.CatalogContext.ValueObjects.Ids.ItemId;

namespace OIO.Infrastructure.Persistence.Configurations.AuctionContext;

internal sealed class AuctionConfiguration : IEntityTypeConfiguration<Auction>
{
    public void Configure(EntityTypeBuilder<Auction> builder)
    {
        builder.ToTable("auctions", t =>
        {
            t.HasCheckConstraint("chk_end_after_start", "end_time > start_time");
            t.HasCheckConstraint("chk_reserve_gte_starting", "reserve_price IS NULL OR reserve_price >= starting_price");
            t.HasCheckConstraint("chk_buy_now_gt_starting", "buy_now_price IS NULL OR buy_now_price > starting_price");
            t.HasCheckConstraint("chk_current_gte_starting", "current_price >= starting_price");
            t.HasCheckConstraint("chk_positive_bid_increment", "bid_increment > 0");
            t.HasCheckConstraint("chk_qualification_window", "qualification_start_at IS NULL OR qualification_end_at IS NULL OR qualification_end_at > qualification_start_at");
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

        builder.ComplexProperty(auction => auction.AuctionType, auctionTypeBuilder =>
        {
            auctionTypeBuilder.Property(auctionType => auctionType.Id)
                .HasColumnName("auction_type")
                .HasDefaultValue(AuctionType.Regular.Id)
                .IsRequired();
        });

        builder.ComplexProperty(a => a.Pricing, pricing =>
        {
            pricing.ComplexProperty(p => p.StartingPrice, startingPrice =>
            {
                startingPrice.Property(cp => cp.Amount)
                    .HasColumnName("starting_price")
                    .HasColumnType("numeric(18,2)")
                    .IsRequired();

                startingPrice.Ignore(cp => cp.Currency);
            });
            
            pricing.ComplexProperty(p => p.ReservePrice, reservePrice =>
            {
                reservePrice.Property(cp => cp.Amount)
                    .HasColumnName("reserve_price")
                    .HasColumnType("numeric(18,2)");

                reservePrice.Ignore(cp => cp.Currency);
            });
            
            pricing.ComplexProperty(p => p.BuyNowPrice, buyNowPrice =>
            {
                buyNowPrice.Property(cp => cp.Amount)
                    .HasColumnName("buy_now_price")
                    .HasColumnType("numeric(18,2)");

                buyNowPrice.Ignore(cp => cp.Currency);
            });

            pricing.ComplexProperty(p => p.CurrentPrice, currentPrice =>
            {
                currentPrice.Property(cp => cp.Amount)
                    .HasColumnName("current_price")
                    .HasColumnType("numeric(18,2)")
                    .IsRequired();

                currentPrice.Ignore(cp => cp.Currency);
            });

            pricing.ComplexProperty(p => p.BidIncrement, bidIncrement =>
            {
                bidIncrement.Property(cp => cp.Amount)
                    .HasColumnName("bid_increment")
                    .HasColumnType("numeric(18,2)")
                    .IsRequired();

                bidIncrement.Ignore(cp => cp.Currency);
            });
            
            pricing.ComplexProperty(p => p.Currency, currency =>
            {
                currency.Property(cp => cp.Id)
                    .HasColumnName("currency")
                    .IsRequired();

                currency.Ignore(cp => cp.Symbol);
            });

            
            pricing.Ignore(p => p.NextMinimumBid);
            pricing.Ignore(p => p.HasBuyNowPrice);
            pricing.Ignore(p => p.HasReservePrice);
            pricing.Ignore(p => p.ReserveMet);
            pricing.Ignore(p => p.IsBuyNowAvailable);
        });

        builder.ComplexProperty(a => a.Info, info =>
        {
            info.Property(i => i.StartTime)
                .HasColumnName("start_time")
                .IsRequired();

            info.Property(i => i.EndTime)
                .HasColumnName("end_time")
                .IsRequired();

            info.Property(i => i.AutoExtend)
                .HasColumnName("auto_extend");

            info.Property(i => i.ExtensionMinutes)
                .HasColumnName("extension_minutes");

            // Nested VO: QualificationWindow? inside AuctionInfo
            info.ComplexProperty(i => i.Qualification, qual =>
            {
                qual.Property(q => q.StartTime)
                    .HasColumnName("qualification_start_at");

                qual.Property(q => q.EndTime)
                    .HasColumnName("qualification_end_at");
            });
        }).HasComplexCompositeIndex(
            a => new { a.Info.StartTime, a.Info.EndTime },
            filter: "((status)::text = 'active'::text)",
            indexName: "idx_auctions_active");

        // ==================== Status ====================
        builder.ComplexProperty(x => x.Status, statusBuilder =>
        {
            statusBuilder.Property(a => a.Id)
                .HasColumnName("status")
                .HasMaxLength(20)
                .HasDefaultValue(AuctionStatus.Draft.Id)
                .IsRequired()
                .HasComplexIndex(indexName: "idx_auctions_status");
        });

        builder.ComplexProperty(x => x.Priority, priorityBuilder =>
        {
            priorityBuilder.Property(p => p.Score)
                .HasColumnName("priority")
                .IsRequired()
                .HasComplexIndex(indexName: "idx_auctions_priority");
            
            priorityBuilder.Property(p => p.Reason)
                .HasColumnName("priority_reason")
                .HasColumnType("jsonb")
                .IsRequired();
        });

        builder.Property(a => a.WinnerId)
            .HasColumnName("winner_id")
            .HasConversion(x => x.HasValue ? (Guid?)x.Value : null , x => x.HasValue ? UserId.From(x.Value) : null);

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

        builder.Property(a => a.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.Property(a => a.ModifiedAt)
            .HasColumnName("modified_at");

        // ==================== Relationships ====================
        builder.HasMany(a => a.Bids)
            .WithOne(b => b.Auction)
            .HasForeignKey(b => b.AuctionId);

        builder.HasMany(a => a.AutoBids)
            .WithOne(ab => ab.Auction)
            .HasForeignKey(ab => ab.AuctionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(a => a.Watchers)
            .WithOne(w => w.Auction)
            .HasForeignKey(w => w.AuctionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(a => a.PriceHistories)
            .WithOne(ph => ph.Auction)
            .HasForeignKey(ph => ph.AuctionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(a => a.Deposits)
            .WithOne(d => d.Auction)
            .HasForeignKey(d => d.AuctionId);

        builder.HasMany(a => a.Participants)
            .WithOne(ap => ap.Auction)
            .HasForeignKey(p => p.AuctionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(a => a.SealedBids)
            .WithOne(sb => sb.Auction)
            .HasForeignKey(sb => sb.AuctionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(a => a.WinnerOffers)
            .WithOne(wo => wo.Auction)
            .HasForeignKey(wo => wo.AuctionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(a => a.RelistHistories)
            .WithOne(rh => rh.Auction)
            .HasForeignKey(rh => rh.AuctionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(a => a.Emergencies)
            .WithOne(ae => ae.Auction)
            .HasForeignKey(e => e.AuctionId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasOne(a => a.Item)
            .WithMany()
            .HasForeignKey(a => a.ItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}