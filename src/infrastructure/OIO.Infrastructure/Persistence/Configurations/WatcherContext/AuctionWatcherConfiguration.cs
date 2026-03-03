using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;

namespace OIO.Infrastructure.Persistence.Configurations.WatcherContext;

public sealed class AuctionWatcherConfiguration : IEntityTypeConfiguration<AuctionWatcher>
{
    public void Configure(EntityTypeBuilder<AuctionWatcher> builder)
    {
        builder.ToTable("auction_watchers");

        // Primary Key
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, v => AuctionWatcherId.From(v))
            .HasColumnName("id")
            .HasDefaultValueSql("uuidv7()");

        // Foreign Keys
        builder.Property(x => x.AuctionId)
            .HasConversion(id => id.Value, v => AuctionId.From(v))
            .HasColumnName("auction_id")
            .IsRequired();

        builder.Property(x => x.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        // Notify Flags with Defaults
        builder.Property(x => x.NotifyOnBid)
            .HasColumnName("notify_on_bid")
            .HasDefaultValue(true);

        builder.Property(x => x.NotifyOnEnd)
            .HasColumnName("notify_on_end")
            .HasDefaultValue(true);

        // Created At
        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        // --- Unique Index: (auction_id, user_id) ---
        // Khớp với: auction_watchers_auction_id_user_id_key
        builder.HasIndex(x => new { x.AuctionId, x.UserId })
            .IsUnique()
            .HasDatabaseName("auction_watchers_auction_id_user_id_key");
    }
}