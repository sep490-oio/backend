using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OIO.Domain.Context.AuctionContext.Aggregates.Bids;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;

namespace OIO.Infrastructure.Persistence.Configurations.BidContext;

public sealed class BidConfiguration : IEntityTypeConfiguration<Bid>
{
    public void Configure(EntityTypeBuilder<Bid> builder)
    {
        builder.ToTable("bids");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => BidId.From(value));

        builder.Property(x => x.AuctionId)
            .HasConversion(id => id.Value, value => AuctionId.From(value));

        builder.OwnsOne(x => x.Amount, m =>
        {
            m.Property(p => p.Amount).HasColumnName("amount").HasPrecision(18, 2);
            m.Property(p => p.Currency).HasColumnName("currency").HasMaxLength(10);
        });

        builder.Property(x => x.Status).HasConversion<int>();
    }
}