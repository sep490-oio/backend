using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.OrderContext.Enums;
using OIO.Infrastructure.Persistence.Converters;

namespace OIO.Infrastructure.Persistence.Configurations.OrderContext;

internal sealed class OrderReturnConfiguration : IEntityTypeConfiguration<OrderReturn>
{
    public void Configure(EntityTypeBuilder<OrderReturn> builder)
    {
        builder.ToTable("order_returns");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.OrderId)
            .HasColumnName("order_id")
            .IsRequired();

        builder.Property(r => r.BuyerId)
            .HasColumnName("buyer_id")
            .IsRequired();

        builder.Property(r => r.ReasonCode)
            .HasColumnName("reason_code")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(r => r.Description)
            .HasColumnName("description");

        builder.ComplexProperty(r => r.Status, statusBuilder =>
        {
            statusBuilder.Property(s => s.Id)
                .HasColumnName("status")
                .HasMaxLength(30)
                .IsRequired();
        });

        builder.Property(r => r.RequestedAt)
            .HasColumnName("requested_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.Property(r => r.ApprovedAt)
            .HasColumnName("approved_at");

        builder.Property(r => r.RejectedAt)
            .HasColumnName("rejected_at");

        builder.Property(r => r.DecisionReason)
            .HasColumnName("decision_reason");

        builder.Property(r => r.ProviderCode)
            .HasColumnName("provider_code")
            .HasMaxLength(50);

        builder.Property(r => r.TrackingNumber)
            .HasColumnName("tracking_number")
            .HasMaxLength(100);

        builder.Property(r => r.ShippedAt)
            .HasColumnName("shipped_at");

        builder.Property(r => r.ReturnedAt)
            .HasColumnName("returned_at");

        builder.Property(r => r.SellerReceivedAt)
            .HasColumnName("seller_received_at");

        builder.Property(r => r.SellerConfirmedReceivedAt)
            .HasColumnName("seller_confirmed_received_at");

        builder.Property(r => r.BuyerDecisionDueAt)
            .HasColumnName("buyer_decision_due_at");

        // Who pays the return-shipping fee (set by dispute-resolution open-return flow).
        // Optional — mapped as a scalar column via the EnumValueObject converter, so EF
        // can distinguish NULL (no fee-payer chosen) from an empty flattened complex value.
        builder.Property(r => r.ShippingFeePayer)
            .HasColumnName("shipping_fee_payer")
            .HasConversion(
                p => p == null ? null : p.Id,
                id => id == null ? null : ShippingFeePayer.FromId(id).Value)
            .HasMaxLength(20);

        // Deferred-refund intent + amount — populated by Order.OpenReturnViaDispute
        // when dispute-resolve opens a pre-approved return. Refund fires later at
        // seller-confirm via RefundDecisionPolicy.
        builder.Property(r => r.DeferredRefundIntent)
            .HasColumnName("deferred_refund_intent")
            .HasConversion(
                p => p == null ? null : p.Id,
                id => id == null ? null : DeferredRefundIntent.FromId(id).Value)
            .HasMaxLength(20);

        builder.Property(r => r.DeferredRefundAmount)
            .HasColumnName("deferred_refund_amount")
            .HasColumnType("numeric(18,2)");

        // Signed return-scoped QR token, issued at MarkReturnShipped time.
        builder.Property(r => r.QrToken)
            .HasColumnName("qr_token")
            .HasMaxLength(512);

        builder.Property(r => r.LastReminderSentAt)
            .HasColumnName("last_reminder_sent_at");

        // Evidence — 1-to-many to OrderReturnEvidence.
        builder.HasMany(r => r.Evidence)
            .WithOne()
            .HasForeignKey(ev => ev.OrderReturnId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        // Partial unique: at most one ACTIVE return per order. Terminal rows remain
        // for audit, allowing a new return after the prior one resolves/cancels/rejects.
        builder.HasIndex(r => r.OrderId)
            .IsUnique()
            .HasDatabaseName("uq_order_returns_order")
            .HasFilter("status NOT IN ('resolved','cancelled','rejected')");

        builder.HasIndex(r => r.BuyerId)
            .HasDatabaseName("idx_order_returns_buyer");

        builder.HasIndex(r => r.TrackingNumber)
            .HasDatabaseName("idx_order_returns_tracking_number");
    }
}
