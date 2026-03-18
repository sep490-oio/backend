using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;

namespace OIO.Infrastructure.Persistence.Configurations.AuctionContext;

internal sealed class AuctionWatcherConfiguration : IEntityTypeConfiguration<AuctionWatcher>
{
    public void Configure(EntityTypeBuilder<AuctionWatcher> builder)
    {
        builder.ToTable("auction_watchers");

        builder.HasKey(w => w.Id);

        builder.Property(w => w.Id)
            .HasColumnName("id")
            .ValueGeneratedNever()
            .HasConversion(x => x.Value, x => AuctionWatcherId.From(x));

        builder.Property(w => w.AuctionId)
            .HasColumnName("auction_id")
            .IsRequired()
            .HasConversion(x => x.Value, x => AuctionId.From(x));

        builder.Property(w => w.UserId)
            .HasColumnName("user_id")
            .IsRequired()
            .HasConversion(x => x.Value, x => UserId.From(x));

        builder.Property(w => w.NotifyOnBid)
            .HasColumnName("notify_on_bid")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(w => w.NotifyOnEnd)
            .HasColumnName("notify_on_end")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(w => w.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        // UNIQUE(auction_id, user_id)
        builder.HasIndex(w => new { w.AuctionId, w.UserId })
            .IsUnique();
    }
}