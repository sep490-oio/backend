using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.Context.WarehouseContext.Aggregates.OutboundShipments;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseItems;
using OIO.Domain.Context.WarehouseContext.Enums;
using OIO.Domain.Context.WarehouseContext.ValueObjects;
using OIO.Domain.Context.WarehouseContext.ValueObjects.Ids;

namespace OIO.Infrastructure.Persistence.Configurations.WarehouseContext;

internal sealed class OutboundShipmentConfiguration : IEntityTypeConfiguration<OutboundShipment>
{
    public void Configure(EntityTypeBuilder<OutboundShipment> builder)
    {
        builder.ToTable("outbound_shipments", t =>
        {
            t.HasCheckConstraint(
                "chk_outbound_shipments_cod_amount",
                "cod_amount >= 0");

            t.HasCheckConstraint(
                "chk_outbound_shipments_shipping_fee",
                "shipping_fee >= 0");

            t.HasCheckConstraint(
                "chk_outbound_shipments_insurance_value",
                "insurance_value >= 0");
        });

        // ==================== Primary Key ====================
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .ValueGeneratedNever()
            .HasColumnName("id")
            .HasConversion(x => x.Value, v => OutboundShipmentId.From(v));

        // ==================== Properties ====================
        // Soft FK to orders table — text address read from orders, not duplicated here
        builder.Property(e => e.OrderId)
            .HasColumnName("order_id")
            .IsRequired();

        builder.Property(e => e.WarehouseItemId)
            .HasColumnName("warehouse_item_id")
            .IsRequired(false)
            .HasConversion(x => x.HasValue ? x.Value.Value : (Guid?)null, v => v.HasValue ? WarehouseItemId.From(v.Value) : null);

        builder.Property(e => e.ShipmentMode)
            .HasColumnName("shipment_mode")
            .HasMaxLength(30)
            .IsRequired()
            .HasConversion(x => x.Id, v => OutboundShipmentMode.FromId(v).GetValueOrThrow());

        builder.Property(e => e.ExternalCarrierName)
            .HasColumnName("external_carrier_name")
            .HasMaxLength(100);

        builder.Property(e => e.ProviderCode)
            .HasColumnName("provider_code")
            .HasMaxLength(20)
            .IsRequired()
            .HasConversion(x => x.Id, v => ShippingProviderCode.FromId(v).GetValueOrThrow());

        builder.Property(e => e.ClientOrderCode)
            .HasColumnName("client_order_code")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.CarrierTrackingNumber)
            .HasColumnName("carrier_tracking_number")
            .HasMaxLength(100);

        builder.Property(e => e.ShippingLabelUrl)
            .HasColumnName("shipping_label_url")
            .HasMaxLength(500);

        builder.Property(e => e.ShippingMethod)
            .HasColumnName("shipping_method")
            .HasMaxLength(50);

        // GHN: { "district_id": 1442, "ward_code": "21012" }. Null for GHTK.
        // Recipient text address is read from orders table via OrderId.
        builder.Property(e => e.RecipientCarrierAddressData)
            .HasColumnName("recipient_carrier_address_data")
            .HasColumnType("jsonb")
            .HasConversion(
                x => x == null ? null : x.RawJson,
                v => v == null ? null : CarrierAddressData.From(v));

        // ==================== Package ====================
        builder.ComplexProperty(e => e.Dimensions, dim =>
        {
            dim.Property(d => d.WeightGrams)
                .HasColumnName("weight_grams")
                .IsRequired();

            dim.Property(d => d.LengthCm)
                .HasColumnName("length_cm");

            dim.Property(d => d.WidthCm)
                .HasColumnName("width_cm");

            dim.Property(d => d.HeightCm)
                .HasColumnName("height_cm");

            dim.Ignore(d => d.WeightKg);
        });

        builder.Property(e => e.ShippingFee)
            .HasColumnName("shipping_fee")
            .HasColumnType("numeric(18,2)")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(e => e.InsuranceValue)
            .HasColumnName("insurance_value")
            .HasColumnType("numeric(18,2)")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(e => e.CodAmount)
            .HasColumnName("cod_amount")
            .HasColumnType("numeric(18,2)")
            .HasDefaultValue(0)
            .IsRequired();

        // GHN only: 1 = shop pays, 2 = buyer pays. Null for GHTK.
        builder.Property(e => e.GhnPaymentType)
            .HasColumnName("ghn_payment_type")
            .HasMaxLength(5)
            .HasConversion(
                x => x == null ? null : x.Id,
                v => v == null ? null : GhnPaymentType.FromId(v).GetValueOrThrow());

        // GHN only: CHOTHUHANG | CHOXEMHANGKHONGTHU | KHONGCHOXEMHANG. Null for GHTK.
        builder.Property(e => e.GhnHandlingNote)
            .HasColumnName("ghn_handling_note")
            .HasMaxLength(30)
            .HasConversion(
                x => x == null ? null : x.Id,
                v => v == null ? null : GhnHandlingNote.FromId(v).GetValueOrThrow());

        builder.Property(e => e.ExtraData)
            .HasColumnName("extra_data")
            .HasColumnType("jsonb")
            .IsRequired()
            .HasConversion(x => x.RawJson, v => ShipmentExtraData.From(v));

        // ==================== Status & Metadata ====================
        builder.Property(e => e.Status)
            .HasColumnName("status")
            .HasMaxLength(30)
            .IsRequired()
            .HasConversion(x => x.Id, v => OutboundShipmentStatus.FromId(v).GetValueOrThrow());

        builder.Property(e => e.PackedBy)
            .HasColumnName("packed_by")
            .HasConversion(
                x => x.HasValue ? x.Value.Value : (Guid?)null,
                v => v.HasValue ? UserId.From(v.Value) : null);

        builder.Property(e => e.PackedAt)
            .HasColumnName("packed_at");

        builder.Property(e => e.DispatchedAt)
            .HasColumnName("dispatched_at");

        builder.Property(e => e.EstimatedDeliveryAt)
            .HasColumnName("estimated_delivery_at");

        builder.Property(e => e.DeliveredAt)
            .HasColumnName("delivered_at");

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.Property(e => e.ModifiedAt)
            .HasColumnName("modified_at");

        // ==================== Relationships ====================
        // EF FK to warehouse_items (1-to-1 — each item ships exactly once outbound)
        builder.HasOne<WarehouseItem>()
            .WithOne()
            .HasForeignKey<OutboundShipment>(e => e.WarehouseItemId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.TrackingEvents)
            .WithOne()
            .HasForeignKey("OutboundShipmentId")
            .OnDelete(DeleteBehavior.Cascade);

        // ==================== Indexes ====================
        builder.HasIndex(e => e.ClientOrderCode)
            .IsUnique()
            .HasDatabaseName("idx_unique_outbound_shipments_client_order_code");

        builder.HasIndex(e => e.WarehouseItemId)
            .IsUnique()
            .HasDatabaseName("idx_unique_outbound_shipments_warehouse_item_id");

        builder.HasIndex(e => e.CarrierTrackingNumber)
            .HasDatabaseName("idx_outbound_shipments_carrier_tracking_number")
            .HasFilter("carrier_tracking_number IS NOT NULL");

        builder.HasIndex(e => e.OrderId)
            .HasDatabaseName("idx_outbound_shipments_order_id");

        builder.HasIndex(e => e.Status)
            .HasDatabaseName("idx_outbound_shipments_status");

        // ==================== Ignore ====================
        builder.Ignore(e => e.DomainEvents);
    }
}