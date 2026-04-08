using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.OrderContext.Aggregates.SellerDirectShipments;
using OIO.Domain.Context.OrderContext.Enums;

namespace OIO.Infrastructure.Persistence.Configurations.OrderContext;

internal sealed class SellerDirectShipmentConfiguration : IEntityTypeConfiguration<SellerDirectShipment>
{
    public void Configure(EntityTypeBuilder<SellerDirectShipment> builder)
    {
        builder.ToTable("seller_direct_shipments");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .ValueGeneratedNever()
            .HasColumnName("id");

        builder.Property(s => s.OrderId)
            .HasColumnName("order_id")
            .IsRequired();

        builder.Property(s => s.ShipmentIdDisplay)
            .HasColumnName("shipment_id_display")
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(s => s.InternalTrackingCode)
            .HasColumnName("internal_tracking_code")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(s => s.QrPayload)
            .HasColumnName("qr_payload")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(s => s.QrCodeUrl)
            .HasColumnName("qr_code_url")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(s => s.ExternalCarrierName)
            .HasColumnName("external_carrier_name")
            .HasMaxLength(100);

        builder.Property(s => s.ExternalTrackingCode)
            .HasColumnName("external_tracking_code")
            .HasMaxLength(100);

        builder.ComplexProperty(s => s.Status, statusBuilder =>
        {
            statusBuilder.Property(x => x.Id)
                .HasColumnName("status")
                .HasMaxLength(30)
                .IsRequired();
        });

        builder.Property(s => s.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.Property(s => s.ModifiedAt)
            .HasColumnName("modified_at");

        builder.Property(s => s.CarrierBookedAt)
            .HasColumnName("carrier_booked_at");

        builder.Property(s => s.PickedUpAt)
            .HasColumnName("picked_up_at");

        builder.Property(s => s.OnDeliveringAt)
            .HasColumnName("on_delivering_at");

        builder.Property(s => s.DeliveredAt)
            .HasColumnName("delivered_at");

        builder.Property(s => s.BuyerReceivedPackageAt)
            .HasColumnName("buyer_received_package_at");

        builder.Property(s => s.BuyerAcceptedAt)
            .HasColumnName("buyer_accepted_at");

        builder.Property(s => s.DisputedAt)
            .HasColumnName("disputed_at");

        builder.Property(s => s.CompletedAt)
            .HasColumnName("completed_at");

        builder.Property(s => s.QrTokenVersion)
            .HasColumnName("qr_token_version")
            .HasDefaultValue(1)
            .IsRequired();

        builder.Property(s => s.QrTokenIssuedAt)
            .HasColumnName("qr_token_issued_at");

        builder.Property(s => s.QrTokenRevokedAt)
            .HasColumnName("qr_token_revoked_at");

        builder.Property(s => s.SellerDeclaredShippedAt)
            .HasColumnName("seller_declared_shipped_at");

        builder.Property(s => s.BuyerPackageCondition)
            .HasColumnName("buyer_package_condition")
            .HasMaxLength(32);

        builder.Property(s => s.BuyerConditionNotes)
            .HasColumnName("buyer_condition_notes")
            .HasMaxLength(2000);

        builder.Property(s => s.ManualReviewRequired)
            .HasColumnName("manual_review_required")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(s => s.ManualReviewReason)
            .HasColumnName("manual_review_reason")
            .HasMaxLength(256);

        builder.Metadata
            .FindNavigation(nameof(SellerDirectShipment.Evidence))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        // Relationships — strict 1:1 with Order via order_id.
        builder.HasOne<Order>()
            .WithOne()
            .HasForeignKey<SellerDirectShipment>(s => s.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(s => s.OrderId)
            .IsUnique()
            .HasDatabaseName("uq_seller_direct_shipments_order_id");

        builder.HasIndex(s => s.ShipmentIdDisplay)
            .IsUnique()
            .HasDatabaseName("uq_seller_direct_shipments_shipment_id_display");

        builder.HasIndex(s => s.InternalTrackingCode)
            .IsUnique()
            .HasDatabaseName("uq_seller_direct_shipments_internal_tracking_code");

        builder.Ignore(s => s.DomainEvents);
    }
}
