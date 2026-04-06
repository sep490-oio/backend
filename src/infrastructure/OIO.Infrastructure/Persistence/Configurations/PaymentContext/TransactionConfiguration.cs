using EFCore.ComplexIndexes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OIO.Domain.Context.PaymentContext.Aggregates.Transactions;

namespace OIO.Infrastructure.Persistence.Configurations.PaymentContext;

internal sealed class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("transactions");

        builder.HasKey(t => t.Id);

        builder.ComplexProperty(t => t.TransactionNumber, tn =>
        {
            tn.Property(x => x.Value)
                .HasColumnName("transaction_number")
                .HasMaxLength(50)
                .IsRequired()
                .HasComplexIndex(isUnique: true);
        });

        builder.Property(t => t.OrderId)
            .HasColumnName("order_id");

        builder.Property(t => t.AuctionId)
            .HasColumnName("auction_id");

        builder.Property(t => t.BuyNowReservationId)
            .HasColumnName("buy_now_reservation_id");

        builder.Property(t => t.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(t => t.PaymentMethodId)
            .HasColumnName("payment_method_id");

        builder.ComplexProperty(t => t.Type, typeBuilder =>
        {
            typeBuilder.Property(x => x.Id)
                .HasColumnName("type")
                .HasMaxLength(20)
                .IsRequired();
        });

        builder.ComplexProperty(t => t.Amount, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("amount")
                .HasColumnType("numeric(18,2)")
                .IsRequired();

            money.ComplexProperty(m => m.Currency, c =>
            {
                c.Property(x => x.Id)
                    .HasColumnName("amount_currency")
                    .HasMaxLength(3);

                c.Ignore(x => x.Symbol);
            });
        });

        builder.Property(t => t.Fee)
            .HasColumnName("fee")
            .HasColumnType("numeric(18,2)")
            .HasDefaultValue(0m);

        builder.ComplexProperty(t => t.NetAmount, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("net_amount")
                .HasColumnType("numeric(18,2)")
                .IsRequired();

            money.ComplexProperty(m => m.Currency, c =>
            {
                c.Property(x => x.Id)
                    .HasColumnName("net_amount_currency")
                    .HasMaxLength(3);

                c.Ignore(x => x.Symbol);
            });
        });

        builder.Property(t => t.Currency)
            .HasColumnName("currency")
            .HasMaxLength(3)
            .HasDefaultValue("VND");

        builder.ComplexProperty(t => t.Status, statusBuilder =>
        {
            statusBuilder.Property(s => s.Id)
                .HasColumnName("status")
                .HasMaxLength(20)
                .IsRequired()
                .HasComplexIndex(indexName: "idx_transactions_status");
        });

        builder.ComplexProperty(t => t.Gateway, g =>
        {
            g.Property(x => x.Provider)
                .HasColumnName("gateway_provider")
                .HasMaxLength(50);

            g.Property(x => x.TransactionId)
                .HasColumnName("gateway_transaction_id")
                .HasMaxLength(255);

            g.Property(x => x.Response)
                .HasColumnName("gateway_response")
                .HasColumnType("jsonb");
        });

        builder.Property(t => t.Description)
            .HasColumnName("description");

        builder.Property(t => t.ClientReturnPath)
            .HasColumnName("client_return_path")
            .HasMaxLength(500);

        builder.Property(t => t.ProcessedAt)
            .HasColumnName("processed_at");

        builder.Property(t => t.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        // Navigation
        builder.HasOne(t => t.PaymentMethod)
            .WithMany()
            .HasForeignKey(t => t.PaymentMethodId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes
        builder.HasIndex(t => t.UserId)
            .HasDatabaseName("idx_transactions_user");

        builder.HasIndex(t => t.OrderId)
            .HasDatabaseName("idx_transactions_order");

        builder.HasIndex(t => t.AuctionId)
            .HasDatabaseName("idx_transactions_auction");

        builder.HasIndex(t => t.BuyNowReservationId)
            .HasDatabaseName("idx_transactions_buy_now_reservation");
    }
}
