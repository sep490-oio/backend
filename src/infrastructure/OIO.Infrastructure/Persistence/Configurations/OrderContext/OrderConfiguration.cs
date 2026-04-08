using EFCore.ComplexIndexes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;

namespace OIO.Infrastructure.Persistence.Configurations.OrderContext;

internal sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders");

        builder.HasKey(o => o.Id);

        builder.ComplexProperty(o => o.OrderNumber, on =>
        {
            on.Property(x => x.Value)
                .HasColumnName("order_number")
                .HasMaxLength(50)
                .IsRequired()
                .HasComplexIndex(isUnique: true);
        });

        builder.Property(o => o.AuctionId)
            .HasColumnName("auction_id")
            .IsRequired();

        builder.Property(o => o.BuyerId)
            .HasColumnName("buyer_id")
            .IsRequired();

        builder.Property(o => o.SellerId)
            .HasColumnName("seller_id")
            .IsRequired();

        builder.Property(o => o.ShippingAddressId)
            .HasColumnName("shipping_address_id");

        builder.ComplexProperty(o => o.Shipping, s =>
        {
            s.Property(x => x.RecipientName)
                .HasColumnName("shipping_recipient_name")
                .HasMaxLength(100);

            s.Property(x => x.Phone)
                .HasColumnName("shipping_phone")
                .HasMaxLength(20);

            s.Property(x => x.Address)
                .HasColumnName("shipping_address")
                .IsRequired();

            s.Property(x => x.Street)
                .HasColumnName("shipping_street")
                .HasMaxLength(255);

            s.Property(x => x.Ward)
                .HasColumnName("shipping_ward")
                .HasMaxLength(100);

            s.Property(x => x.District)
                .HasColumnName("shipping_district")
                .HasMaxLength(100);

            s.Property(x => x.City)
                .HasColumnName("shipping_city")
                .HasMaxLength(120);

            s.Property(x => x.PostalCode)
                .HasColumnName("shipping_postal_code")
                .HasMaxLength(10);
        });

        builder.Property(o => o.BillingAddressId)
            .HasColumnName("billing_address_id");

        builder.ComplexProperty(o => o.Pricing, p =>
        {
            p.ComplexProperty(x => x.ItemPrice, ip =>
            {
                ip.Property(m => m.Amount)
                    .HasColumnName("item_price")
                    .HasColumnType("numeric(18,2)")
                    .IsRequired();

                ip.ComplexProperty(m => m.Currency, c =>
                {
                    c.Property(cc => cc.Id)
                        .HasColumnName("item_price_currency")
                        .HasMaxLength(3);

                    c.Ignore(x => x.Symbol);
                });
            });

            p.Property(x => x.ShippingFee)
                .HasColumnName("shipping_fee")
                .HasColumnType("numeric(18,2)")
                .HasDefaultValue(0m);

            p.Property(x => x.PlatformFee)
                .HasColumnName("platform_fee")
                .HasColumnType("numeric(18,2)")
                .HasDefaultValue(0m);

            p.Property(x => x.TaxAmount)
                .HasColumnName("tax_amount")
                .HasColumnType("numeric(18,2)")
                .HasDefaultValue(0m);

            p.ComplexProperty(x => x.TotalAmount, ta =>
            {
                ta.Property(m => m.Amount)
                    .HasColumnName("total_amount")
                    .HasColumnType("numeric(18,2)")
                    .IsRequired();

                ta.ComplexProperty(m => m.Currency, c =>
                {
                    c.Property(cc => cc.Id)
                        .HasColumnName("total_amount_currency")
                        .HasMaxLength(3);

                    c.Ignore(x => x.Symbol);
                });
            });
        });

        builder.Property(o => o.Currency)
            .HasColumnName("currency")
            .HasMaxLength(3)
            .HasDefaultValue("VND");

        builder.ComplexProperty(o => o.Status, statusBuilder =>
        {
            statusBuilder.Property(s => s.Id)
                .HasColumnName("status")
                .HasMaxLength(30)
                .IsRequired()
                .HasComplexIndex(indexName: "idx_orders_status");
        });

        builder.Property(o => o.PaymentDueAt)
            .HasColumnName("payment_due_at");

        builder.Property(o => o.PaymentAttemptCount)
            .HasColumnName("payment_attempt_count")
            .HasDefaultValue(0);

        builder.Property(o => o.LastPaymentAttemptAt)
            .HasColumnName("last_payment_attempt_at");

        builder.Property(o => o.PaymentFailureReason)
            .HasColumnName("payment_failure_reason");

        builder.Property(o => o.PaidAt)
            .HasColumnName("paid_at");

        builder.Property(o => o.ShippedAt)
            .HasColumnName("shipped_at");

        builder.Property(o => o.DeliveredAt)
            .HasColumnName("delivered_at");

        builder.Property(o => o.DecisionWindowEndsAt)
            .HasColumnName("decision_window_ends_at");

        builder.Property(o => o.DisputedAt)
            .HasColumnName("disputed_at");

        builder.Property(o => o.CompletedAt)
            .HasColumnName("completed_at");

        builder.Property(o => o.CancelledAt)
            .HasColumnName("cancelled_at");

        builder.Property(o => o.ShipByAt)
            .HasColumnName("ship_by_at");

        builder.Property(o => o.IsShippingOverdue)
            .HasColumnName("is_shipping_overdue")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(o => o.EscalatedAt)
            .HasColumnName("escalated_at");

        builder.Property(o => o.EscalationReason)
            .HasColumnName("escalation_reason")
            .HasMaxLength(100);

        builder.Property(o => o.Version)
            .HasColumnName("version")
            .HasDefaultValue(0)
            .IsConcurrencyToken();

        builder.Property(o => o.Notes)
            .HasColumnName("notes");

        builder.Property(o => o.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.Property(o => o.ModifiedAt)
            .HasColumnName("modified_at");

        // Navigation
        builder.HasOne(o => o.Return)
            .WithOne(r => r.Order)
            .HasForeignKey<OrderReturn>(r => r.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasMany(o => o.Escrows)
            .WithOne(e => e.Order)
            .HasForeignKey(e => e.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasMany(o => o.OutboundShipments)
            .WithOne(o => o.Order)
            .HasForeignKey(o => o.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(o => o.BuyerId)
            .HasDatabaseName("idx_orders_buyer");

        builder.HasIndex(o => o.SellerId)
            .HasDatabaseName("idx_orders_seller");

        builder.HasIndex(o => o.PaymentDueAt)
            .HasDatabaseName("idx_orders_payment_due_at")
            .HasFilter("status = 'pending_payment'");

        builder.HasIndex(o => o.LastPaymentAttemptAt)
            .HasDatabaseName("idx_orders_last_payment_attempt_at");

        builder.HasIndex(o => o.ShipByAt)
            .HasDatabaseName("idx_orders_overdue_scan")
            .HasFilter("is_shipping_overdue = false AND ship_by_at IS NOT NULL");
    }
}
