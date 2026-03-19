using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.OrderContext.ValueObjects.Ids;
using OIO.Domain.Context.PaymentContext.ValueObjects.Ids;
using OIO.Domain.Context.Shared.Enums;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;

namespace OIO.Infrastructure.Persistence.Configurations.AuctionContext;

internal sealed class AuctionBuyNowReservationConfiguration : IEntityTypeConfiguration<AuctionBuyNowReservation>
{
    public void Configure(EntityTypeBuilder<AuctionBuyNowReservation> builder)
    {
        builder.ToTable("auction_buy_now_reservations");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever()
            .HasConversion(x => x.Value, x => AuctionBuyNowReservationId.From(x));

        builder.Property(x => x.AuctionId)
            .HasColumnName("auction_id")
            .IsRequired()
            .HasConversion(x => x.Value, x => AuctionId.From(x));

        builder.Property(x => x.BuyerId)
            .HasColumnName("buyer_id")
            .IsRequired()
            .HasConversion(x => x.Value, x => UserId.From(x));

        builder.Property(x => x.PaymentTransactionId)
            .HasColumnName("payment_transaction_id")
            .HasConversion(
                x => x.HasValue ? x.Value.Value : default(Guid?),
                x => x.HasValue ? TransactionId.From(x.Value) : null);

        builder.Property(x => x.OrderId)
            .HasColumnName("order_id")
            .HasConversion(
                x => x.HasValue ? x.Value.Value : default(Guid?),
                x => x.HasValue ? OrderId.From(x.Value) : null);

        builder.Property(x => x.BuyNowAmount)
            .HasColumnName("buy_now_price")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.DepositAppliedAmountValue)
            .HasColumnName("deposit_applied_amount")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.GatewayAmountDueValue)
            .HasColumnName("gateway_amount_due")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.Currency)
            .HasColumnName("currency")
            .HasMaxLength(3)
            .HasConversion(x => x.Id, x => Currency.FromId(x).Value)
            .IsRequired();

        builder.Ignore(x => x.BuyNowPrice);
        builder.Ignore(x => x.DepositAppliedAmount);
        builder.Ignore(x => x.GatewayAmountDue);

        builder.ComplexProperty(x => x.Status, status =>
        {
            status.Property(s => s.Id)
                .HasColumnName("status")
                .HasMaxLength(30)
                .IsRequired();
        });

        builder.Property(x => x.ExpiresAt)
            .HasColumnName("expires_at")
            .IsRequired();

        builder.Property(x => x.ReleasedAt)
            .HasColumnName("released_at");

        builder.Property(x => x.FailureReason)
            .HasColumnName("failure_reason");

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.Property(x => x.ModifiedAt)
            .HasColumnName("modified_at");

        builder.HasIndex(x => x.AuctionId)
            .HasDatabaseName("idx_auction_buy_now_reservations_auction_id");

        builder.HasIndex(x => x.BuyerId)
            .HasDatabaseName("idx_auction_buy_now_reservations_buyer_id");

        builder.HasIndex(x => x.ExpiresAt)
            .HasDatabaseName("idx_auction_buy_now_reservations_expires_at");

        builder.HasIndex(x => x.PaymentTransactionId)
            .HasDatabaseName("idx_auction_buy_now_reservations_payment_transaction_id");

        builder.HasOne(x => x.Auction)
            .WithMany(x => x.BuyNowReservations)
            .HasForeignKey(x => x.AuctionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
