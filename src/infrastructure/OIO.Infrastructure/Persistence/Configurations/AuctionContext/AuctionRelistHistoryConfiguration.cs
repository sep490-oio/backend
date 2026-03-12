using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;

namespace OIO.Infrastructure.Persistence.Configurations.AuctionContext;

internal sealed class AuctionRelistHistoryConfiguration : IEntityTypeConfiguration<AuctionRelistHistory>
{
    public void Configure(EntityTypeBuilder<AuctionRelistHistory> builder)
    {
        builder.ToTable("auction_relist_history");

        builder.HasKey(rh => rh.Id);

        builder.Property(rh => rh.Id)
            .HasColumnName("id")
            .ValueGeneratedNever()
            .HasConversion(x => x.Value, x => AuctionRelistHistoryId.From(x));

        builder.Property(rh => rh.AuctionId)
            .HasColumnName("auction_id")
            .IsRequired()
            .HasConversion(x => x.Value, x => AuctionId.From(x));

        builder.Property(rh => rh.RelistNo)
            .HasColumnName("relist_no")
            .IsRequired();

        builder.Property(rh => rh.Reason)
            .HasColumnName("reason");

        builder.Property(rh => rh.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();
        
        builder.HasIndex(rh => rh.CreatedAt)
            .HasDatabaseName("idx_auction_relist_history_created_at");
        
        builder.HasIndex(rh => new { rh.AuctionId, rh.RelistNo })
            .HasDatabaseName("idx_auction_relist_history_auction_id")
            .IsUnique();;
    }
}

