using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;

namespace OIO.Infrastructure.Persistence.Configurations.AuctionContext;

internal sealed class AuctionPriceHistoryConfiguration : IEntityTypeConfiguration<AuctionPriceHistory>
{
    public void Configure(EntityTypeBuilder<AuctionPriceHistory> builder)
    {
        builder.ToTable("auction_price_history");

        builder.HasKey(ph => ph.Id);

        builder.Property(ph => ph.Id)
            .HasColumnName("id")
            .ValueGeneratedNever()
            .HasConversion(x => x.Value, x => AuctionPriceHistoryId.From(x));

        builder.Property(ph => ph.AuctionId)
            .HasColumnName("auction_id")
            .IsRequired()
            .HasConversion(x => x.Value, x => AuctionId.From(x));

        builder.ComplexProperty(ph => ph.Price, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("price")
                .HasColumnType("numeric(18,2)")
                .IsRequired();
            
            money.ComplexProperty(m => m.Currency, currencyBuilder =>
            {
                currencyBuilder.Property(x => x.Id)
                    .HasColumnName("currency")
                    .HasMaxLength(3)
                    .IsRequired();

                currencyBuilder.Ignore(x => x.Symbol);
            });
        });

        builder.Property(ph => ph.BidId)
            .HasColumnName("bid_id")
            .HasConversion(x =>  x.HasValue ? (Guid?)x.Value : null, x => x.HasValue ? BidId.From(x.Value) : null);

        builder.Property(ph => ph.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();
    }
}

