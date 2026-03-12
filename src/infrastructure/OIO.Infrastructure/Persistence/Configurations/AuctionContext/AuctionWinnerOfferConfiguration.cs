using EFCore.ComplexIndexes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Enums;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;

namespace OIO.Infrastructure.Persistence.Configurations.AuctionContext;

internal sealed class AuctionWinnerOfferConfiguration : IEntityTypeConfiguration<AuctionWinnerOffer>
{
    public void Configure(EntityTypeBuilder<AuctionWinnerOffer> builder)
    {
        builder.ToTable("auction_winner_offers");

        builder.HasKey(wo => wo.Id);

        builder.Property(wo => wo.Id)
            .HasColumnName("id")
            .ValueGeneratedNever()
            .HasConversion(x => x.Value, x => AuctionWinnerOfferId.From(x));

        builder.Property(wo => wo.AuctionId)
            .HasColumnName("auction_id")
            .IsRequired()
            .HasConversion(x => x.Value, x => AuctionId.From(x));

        builder.Property(wo => wo.UserId)
            .HasColumnName("user_id")
            .IsRequired()
            .HasConversion(x => x.Value, x => UserId.From(x));

        builder.Property(wo => wo.RankNo)
            .HasColumnName("rank_no")
            .IsRequired();

        builder.ComplexProperty(wo => wo.OfferStatus, statusBuilder =>
        {
            statusBuilder.Property(s => s.Id)
                .HasColumnName("offer_status")
                .HasDefaultValue(WinnerOfferStatus.Offered.Id)
                .IsRequired();
        }).HasComplexCompositeIndex(wo => new { wo.OfferStatus.Id, wo.ExpiresAt }, indexName: "idx_auction_winner_offers_status_expires");

        builder.Property(wo => wo.OfferedAt)
            .HasColumnName("offered_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.Property(wo => wo.ExpiresAt)
            .HasColumnName("expires_at");

        builder.Property(wo => wo.RespondedAt)
            .HasColumnName("responded_at");

        // ==================== Indexes ====================
        builder.HasIndex(wo => new { wo.AuctionId, wo.RankNo })
            .HasDatabaseName("uq_auction_winner_offers_auction_rank")
            .IsUnique();

        builder.HasIndex(wo => new { wo.AuctionId, wo.UserId })
            .HasDatabaseName("uq_auction_winner_offers_auction_user")
            .IsUnique();
    }
}

