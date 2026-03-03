using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using OIO.Domain.Context.AuctionContext.Aggregates.Bids;
using OIO.Domain.Context.AuctionContext.Enums;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Shared;
using System.Net;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;

namespace OIO.Infrastructure.Persistence.Configurations.BidContext;

public sealed class BidConfiguration : IEntityTypeConfiguration<Bid>
{
    private static readonly Currency Vnd = Currency.FromCode("VND");

    public void Configure(EntityTypeBuilder<Bid> builder)
    {
        // ── Table ────────────────────────────────────────────────────────────
        builder.ToTable("bids");

        // ── Primary Key ──────────────────────────────────────────────────────
        builder.HasKey(b => b.Id);

        builder.Property(b => b.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .ValueGeneratedNever()
            .HasConversion(
                id => id.Value,
                value => BidId.From(value));

        // ── AuctionId ────────────────────────────────────────────────────────
        builder.Property(b => b.AuctionId)
            .HasColumnName("auction_id")
            .HasColumnType("uuid")
            .IsRequired()
            .HasConversion(
                id => id.Value,
                value => AuctionId.From(value));

        // ── BidderId (plain Guid — no strong ID wrapper on this aggregate) ───
        builder.Property(b => b.BidderId)
            .HasColumnName("bidder_id")
            .HasColumnType("uuid")
            .IsRequired();

        // ── Amount (Money value object) ──────────────────────────────────────
        builder.Property(b => b.Amount)
            .HasColumnName("amount")
            .HasColumnType("numeric(18,2)")
            .IsRequired()
            .HasConversion(
                m => m.Amount,
                v => new Money(v, Vnd));

        // ── IsAutoBid ────────────────────────────────────────────────────────
        builder.Property(b => b.IsAutoBid)
            .HasColumnName("is_auto_bid")
            .HasColumnType("boolean");

        // ── AutoBidId (nullable strong ID) ───────────────────────────────────
        builder.Property(b => b.AutoBidId)
            .HasColumnName("auto_bid_id")
            .HasColumnType("uuid")
            .IsRequired(false)
            .HasConversion(
                new ValueConverter<AuctionAutoBidId?, Guid?>(
                    id => id == null ? (Guid?)null : (Guid?)id.Value,
                    v  => v == null ? null : AuctionAutoBidId.From(v.Value)));

        // ── Status ───────────────────────────────────────────────────────────
        builder.Property(b => b.Status)
            .HasColumnName("status")
            .HasColumnType("character varying(20)")
            .IsRequired()
            .HasDefaultValue(BidStatus.Active)
            .HasConversion(
                s => s.ToString().ToLowerInvariant(),
                s => Enum.Parse<BidStatus>(s, ignoreCase: true));

        // ── IpAddress ────────────────────────────────────────────────────────
        // PostgreSQL 'inet' has no built-in EF Core mapping; convert via string.
        // ── IpAddress ────────────────────────────────────────────────────────
// Drop the HasConversion string entirely if using PostgreSQL (Npgsql natively maps System.Net.IPAddress to inet).
        builder.Property(b => b.IpAddress)
            .HasColumnName("ip_address")
            .HasColumnType("inet")
            .IsRequired(false);

        // ── CreatedAt ────────────────────────────────────────────────────────
        builder.Property(b => b.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp")
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // ── Indexes ──────────────────────────────────────────────────────────
        builder.HasIndex(b => b.AuctionId)
            .HasDatabaseName("idx_bids_auction");

        builder.HasIndex(b => b.BidderId)
            .HasDatabaseName("idx_bids_bidder");

        builder.HasIndex(b => new { b.AuctionId, b.Amount })
            .HasDatabaseName("idx_bids_amount");

        // ── Check Constraints ────────────────────────────────────────────────
        builder.ToTable(t =>
        {
            t.HasCheckConstraint(
                "bids_status_check",
                "status IN ('active','outbid','winning','won','cancelled')");
        });

        // ── Relationships ────────────────────────────────────────────────────
        builder.HasOne<Auction>()
            .WithMany()
            .HasForeignKey(b => b.AuctionId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("bids_auction_id_fkey");

        // auto_bid_id → auction_auto_bids is a reference but Bid is its own
        // aggregate root, so we do not navigate into AuctionAutoBid here.

        // ── Ignore domain-event collection ───────────────────────────────────
        builder.Ignore(b => b.DomainEvents);
    }
}