using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;

namespace OIO.Infrastructure.Persistence.Configurations.AuctionContext;

internal sealed class BidEventConfiguration : IEntityTypeConfiguration<BidEvent>
{
    public void Configure(EntityTypeBuilder<BidEvent> builder)
    {
        builder.ToTable("bid_events");
        
        builder.HasKey(b => b.Id);
        
        builder.Property(b => b.Id)
            .HasColumnName("id")
            .ValueGeneratedNever()
            .HasConversion(x => x.Value, x => BidEventId.From(x));

        builder.Property(b => b.BidId)
            .HasColumnName("bid_id")
            .IsRequired()
            .HasConversion(x => x.Value, x => BidId.From(x));
        
        builder.Property(b => b.EventType)
            .HasColumnName("event_type")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(b => b.ReasonCode)
            .HasColumnName("reason_code");
        
        builder.Property(b => b.Payload)
            .HasColumnName("payload")
            .HasColumnType("jsonb")
            .IsRequired();
        
        builder.Property(b => b.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();
        
        builder.HasIndex(b => b.BidId)
            .HasDatabaseName("idx_bid_events_bid");
        
        builder.HasIndex(b => new { b.EventType, b.CreatedAt})
            .HasDatabaseName("idx_bid_events_bid_created_at");
    }
}