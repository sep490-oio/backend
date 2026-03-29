using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.Context.CatalogContext.ValueObjects.Ids;
using OIO.Domain.Context.CatalogContext.Aggregates.Items;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.WarehouseContext.Aggregates.InboundShipments;
using OIO.Domain.Context.WarehouseContext.Enums;
using OIO.Domain.Context.WarehouseContext.ValueObjects;
using OIO.Domain.Context.WarehouseContext.ValueObjects.Ids;

namespace OIO.Infrastructure.Persistence.Configurations.WarehouseContext;

internal sealed class InboundShipmentConfiguration : IEntityTypeConfiguration<InboundShipment>
{
    public void Configure(EntityTypeBuilder<InboundShipment> builder)
    {
        builder.ToTable("inbound_shipments");

        // ==================== Primary Key ====================
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .ValueGeneratedNever()
            .HasColumnName("id")
            .HasConversion(x => x.Value, v => InboundShipmentId.From(v));

        // ==================== Soft FK Properties ====================
        // Soft FKs — no EF navigation, domain enforces consistency
        builder.Property(e => e.ItemId)
            .HasColumnName("item_id")
            .IsRequired();

        builder.Property(e => e.SellerId)
            .HasColumnName("seller_id")
            .IsRequired()
            .HasConversion(x => x.Value, v => UserId.From(v));
        // ==================== Shipment Mode ====================
        builder.Property(e => e.ShipmentMode)
            .HasColumnName("shipment_mode")
            .HasMaxLength(20)
            .IsRequired()
            .HasConversion(x => x.Id, v => InboundShipmentMode.FromId(v).GetValueOrThrow());
 
        builder.Property(e => e.ExternalCarrierName)
            .HasColumnName("external_carrier_name")
            .HasMaxLength(100);
        // ==================== Carrier ====================
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

        // ==================== Sender Address ====================
        builder.Property(e => e.SenderName)
            .HasColumnName("sender_name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.SenderPhone)
            .HasColumnName("sender_phone")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(e => e.SenderAddress)
            .HasColumnName("sender_address")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(e => e.SenderWard)
            .HasColumnName("sender_ward")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.SenderDistrict)
            .HasColumnName("sender_district")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.SenderProvince)
            .HasColumnName("sender_province")
            .HasMaxLength(100)
            .IsRequired();

        // GHN: { "district_id": 1442, "ward_code": "21012" }. Null for GHTK.
        builder.Property(e => e.SenderCarrierAddressData)
            .HasColumnName("sender_carrier_address_data")
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

        // Carrier-specific fields: service_type_id, pick_shift, etc.
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
            .HasConversion(x => x.Id, v => InboundShipmentStatus.FromId(v).GetValueOrThrow());

        builder.Property(e => e.Notes)
            .HasColumnName("notes")
            .HasMaxLength(500);

        builder.Property(e => e.ExpectedArrivalAt)
            .HasColumnName("expected_arrival_at");

        builder.Property(e => e.ArrivedAt)
            .HasColumnName("arrived_at");

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.Property(e => e.ModifiedAt)
            .HasColumnName("modified_at");

        // ==================== Relationships ====================
        // Each shipment has an append-only log of tracking events pushed via carrier webhooks.
        // We use a shadow FK "InboundShipmentId" on the ShipmentTrackingEvent table.
        builder.HasMany(e => e.TrackingEvents)
            .WithOne()
            .HasForeignKey("InboundShipmentId")
            .OnDelete(DeleteBehavior.Cascade);

        // NOTE: ClientOrderCode is shared across all InboundShipments in a batch booking.
        // It is NOT unique — N records share the same code (one per batched item).
        builder.HasIndex(e => e.ClientOrderCode)
            .HasDatabaseName("idx_inbound_shipments_client_order_code");

        builder.HasIndex(e => e.CarrierTrackingNumber)
            .HasDatabaseName("idx_inbound_shipments_carrier_tracking_number")
            .HasFilter("carrier_tracking_number IS NOT NULL");

        builder.HasIndex(e => e.ItemId)
            .HasDatabaseName("idx_inbound_shipments_item_id");

        builder.HasIndex(e => e.SellerId)
            .HasDatabaseName("idx_inbound_shipments_seller_id");

        builder.HasIndex(e => e.Status)
            .HasDatabaseName("idx_inbound_shipments_status");
        builder.HasIndex(e => e.ShipmentMode)
            .HasDatabaseName("idx_inbound_shipments_shipment_mode");
        // ==================== Ignore ====================
        builder.Ignore(e => e.DomainEvents);
    }
}