using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;

namespace OIO.Infrastructure.Persistence.Configurations.AuctionContext;

internal sealed class AuctionViewRecordConfiguration : IEntityTypeConfiguration<AuctionViewRecord>
{
    public void Configure(EntityTypeBuilder<AuctionViewRecord> builder)
    {
        builder.ToTable("auction_view_records");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .HasColumnName("id")
            .ValueGeneratedNever()
            .HasConversion(x => x.Value, x => AuctionViewRecordId.From(x));

        builder.Property(r => r.AuctionId)
            .HasColumnName("auction_id")
            .IsRequired()
            .HasConversion(x => x.Value, x => AuctionId.From(x));

        builder.Property(r => r.UserId)
            .HasColumnName("user_id")
            .HasConversion(
                x => x.HasValue ? (Guid?)x.Value : null,
                x => x.HasValue ? UserId.From(x.Value) : null);

        builder.Property(r => r.BrowserViewerId)
            .HasColumnName("browser_viewer_id")
            .HasMaxLength(64);

        builder.Property(r => r.IpHash)
            .HasColumnName("ip_hash")
            .HasMaxLength(16);

        builder.Property(r => r.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.Property(r => r.LastSeenAt)
            .HasColumnName("last_seen_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        // Unique: one view per authenticated user per auction
        builder.HasIndex(r => new { r.AuctionId, r.UserId })
            .HasDatabaseName("IX_auction_view_records_auction_user")
            .IsUnique()
            .HasFilter("user_id IS NOT NULL");

        // Unique: one view per browser viewer per auction
        builder.HasIndex(r => new { r.AuctionId, r.BrowserViewerId })
            .HasDatabaseName("IX_auction_view_records_auction_browser")
            .IsUnique()
            .HasFilter("browser_viewer_id IS NOT NULL");

        // Index for anonymous IP-only fallback
        builder.HasIndex(r => new { r.AuctionId, r.IpHash })
            .HasDatabaseName("IX_auction_view_records_auction_ip")
            .HasFilter("ip_hash IS NOT NULL AND browser_viewer_id IS NULL AND user_id IS NULL");
    }
}
