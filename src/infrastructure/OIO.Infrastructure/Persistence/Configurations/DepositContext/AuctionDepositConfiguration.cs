using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Enums;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Shared;

namespace OIO.Infrastructure.Persistence.Configurations.DepositContext;

public sealed class AuctionDepositConfiguration : IEntityTypeConfiguration<AuctionDeposit>
{
    public void Configure(EntityTypeBuilder<AuctionDeposit> builder)
    {
        builder.ToTable("auction_deposits");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, v => AuctionDepositId.From(v))
            .HasColumnName("id")
            .HasDefaultValueSql("uuidv7()");

        builder.Property(x => x.AuctionId)
            .HasConversion(id => id.Value, v => AuctionId.From(v))
            .HasColumnName("auction_id");

        builder.Property(x => x.UserId).HasColumnName("user_id");

        builder.OwnsOne(x => x.Amount, m =>
        {
            m.Property(p => p.Amount).HasColumnName("amount").HasPrecision(18, 2);
            m.Ignore(p => p.Currency); // Table không có cột currency
        });

        builder.Property(x => x.TransactionId).HasColumnName("transaction_id");

        // Map Enum sang lowercase string để khớp với Check Constraint
        builder.Property(x => x.Status)
            .HasConversion(
                v => v.ToString().ToLower(), 
                v => (DepositStatus)Enum.Parse(typeof(DepositStatus), v, true))
            .HasColumnName("status")
            .HasDefaultValue("held");

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(x => x.ReleasedAt).HasColumnName("released_at");

        // --- Unique Index (auction_id, user_id) ---
        builder.HasIndex(x => new { x.AuctionId, x.UserId })
            .IsUnique()
            .HasDatabaseName("auction_deposits_auction_id_user_id_key");
    }
}