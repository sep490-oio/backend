using EFCore.ComplexIndexes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Enums;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;

namespace OIO.Infrastructure.Persistence.Configurations.AuctionContext;

internal sealed class SealedBidConfiguration : IEntityTypeConfiguration<SealedBid>
{
    public void Configure(EntityTypeBuilder<SealedBid> builder)
    {
        builder.ToTable("sealed_bids");

        builder.HasKey(sb => sb.Id);

        builder.Property(sb => sb.Id)
            .HasColumnName("id")
            .ValueGeneratedNever()
            .HasConversion(x => x.Value, x => SealedBidId.From(x));

        builder.Property(sb => sb.AuctionId)
            .HasColumnName("auction_id")
            .IsRequired()
            .HasConversion(x => x.Value, x => AuctionId.From(x));

        builder.Property(sb => sb.BidderId)
            .HasColumnName("bidder_id")
            .IsRequired()
            .HasConversion(x => x.Value, x => UserId.From(x));

        builder.Property(sb => sb.AmountEncrypted)
            .HasColumnName("amount_encrypted");
        
        builder.ComplexProperty(sb => sb.Status, statusBuilder =>
        {
            statusBuilder.Property(s => s.Id)
                .HasColumnName("status")
                .HasMaxLength(20)
                .HasDefaultValue(SealedBidStatus.Submitted.Id)
                .IsRequired();
        }).HasComplexCompositeIndex(x => new { x.AuctionId, x.Status.Id }, indexName: "idx_sealed_bids_auction_status" );

        builder.Property(sb => sb.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();
        
        builder.Property(sb => sb.RevealedAt)
            .HasColumnName("revealed_at");
        
        builder.Property(sb => sb.RevealedBy)
            .HasColumnName("revealed_by")
            .HasConversion(x => x.HasValue ? x.Value.Value : default(Guid?) , x => x.HasValue ? UserId.From(x.Value) : null);

        // Indexes
        builder.HasIndex(sb => new { sb.AuctionId, sb.BidderId })
            .IsUnique();

        builder.HasIndex(sb => sb.BidderId)
            .HasDatabaseName("idx_sealed_bids_bidder");
        
        builder.HasIndex(sb => new { sb.AuctionId, sb.CreatedAt})
            .HasDatabaseName("idx_sealed_bids_auction_created_at");
    }
}