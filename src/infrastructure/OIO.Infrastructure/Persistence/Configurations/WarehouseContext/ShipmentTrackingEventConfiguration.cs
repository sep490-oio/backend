using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OIO.Domain.Context.WarehouseContext.Aggregates;
using OIO.Domain.Context.WarehouseContext.Enums;
using OIO.Domain.Context.WarehouseContext.ValueObjects;
using OIO.Domain.Context.WarehouseContext.ValueObjects.Ids;

namespace OIO.Infrastructure.Persistence.Configurations.WarehouseContext;

/// <summary>
/// Append-only tracking events shared between inbound and outbound shipments.
///
/// Design note: the DBML used a single polymorphic shipment_id column,
/// but EF Core cannot model two separate FK navigations on the same column.
/// We use two nullable FK columns instead:
///   inbound_shipment_id  — set when shipment_type = 'inbound'
///   outbound_shipment_id — set when shipment_type = 'outbound'
///
/// DB constraint ensures exactly one is set (CHECK in migration raw SQL).
/// shipment_type is kept for easy filtering without joining.
/// </summary>
internal sealed class ShipmentTrackingEventConfiguration : IEntityTypeConfiguration<ShipmentTrackingEvent>
{
    public void Configure(EntityTypeBuilder<ShipmentTrackingEvent> builder)
    {
        builder.ToTable("shipment_tracking_events");

        // ==================== Primary Key ====================
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .ValueGeneratedNever()
            .HasColumnName("id")
            .HasConversion(x => x.Value, v => ShipmentTrackingEventId.From(v));

        // ==================== Discriminator ====================
        // "inbound" or "outbound" — kept for easy filtering without joining
        builder.Property(e => e.ShipmentType)
            .HasColumnName("shipment_type")
            .HasMaxLength(10)
            .IsRequired();

        // Polymorphic ID — the actual value of either inbound or outbound shipment
        // Stored redundantly for query convenience (ORDER BY shipment_id, event_time)
        builder.Property(e => e.ShipmentId)
            .HasColumnName("shipment_id")
            .IsRequired();

        // ==================== Shadow FK Properties ====================
        // EF uses shadow properties for the two nullable FKs.
        // InboundShipmentConfiguration.HasMany(...).HasForeignKey("InboundShipmentId")
        // OutboundShipmentConfiguration.HasMany(...).HasForeignKey("OutboundShipmentId")
        // EF will add InboundShipmentId and OutboundShipmentId shadow columns automatically.

        // ==================== Carrier Data ====================
        builder.Property(e => e.ProviderCode)
            .HasColumnName("provider_code")
            .HasMaxLength(20)
            .IsRequired()
            .HasConversion(x => x.Id, v => ShippingProviderCode.FromId(v).GetValueOrThrow());

        // Exactly what the carrier sent — never transform before storing.
        // GHN: string e.g. "ready_to_pick". GHTK: integer as string e.g. "5".
        builder.Property(e => e.CarrierStatusRaw)
            .HasColumnName("carrier_status_raw")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.CarrierStatusDesc)
            .HasColumnName("carrier_status_desc")
            .HasMaxLength(255);

        builder.Property(e => e.NormalizedStatus)
            .HasColumnName("normalized_status")
            .HasMaxLength(20)
            .IsRequired()
            .HasConversion(x => x.Id, v => NormalizedTrackingStatus.FromId(v).GetValueOrThrow());

        // GHTK: serialized cur_station object. GHN: hub/warehouse name.
        builder.Property(e => e.Location)
            .HasColumnName("location")
            .HasMaxLength(255);

        builder.Property(e => e.ReasonCode)
            .HasColumnName("reason_code")
            .HasMaxLength(50);

        builder.Property(e => e.ReasonDescription)
            .HasColumnName("reason_description")
            .HasMaxLength(500);

        // Carrier's action_time — not our received time
        builder.Property(e => e.EventTime)
            .HasColumnName("event_time")
            .IsRequired();

        // Full webhook body. GHTK form-urlencoded is serialized to JSON by adapter before storing.
        builder.Property(e => e.RawPayload)
            .HasColumnName("raw_payload")
            .HasColumnType("jsonb")
            .IsRequired()
            .HasConversion(x => x.RawJson, v => WebhookRawPayload.From(v));

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();
        // ==================== Shadow FK Properties ====================
        // EF uses shadow properties for the two nullable FKs to avoid polymorphic mapping issues.
        builder.Property<InboundShipmentId?>("InboundShipmentId")
            .HasColumnName("inbound_shipment_id")
            .HasConversion(
                id => id.HasValue ? id.Value.Value : (Guid?)null,
                value => value.HasValue ? InboundShipmentId.From(value.Value) : (InboundShipmentId?)null);

        builder.Property<OutboundShipmentId?>("OutboundShipmentId")
            .HasColumnName("outbound_shipment_id")
            .HasConversion(
                id => id.HasValue ? id.Value.Value : (Guid?)null,
                value => value.HasValue ? OutboundShipmentId.From(value.Value) : (OutboundShipmentId?)null);

        // ==================== Carrier Data ====================
        // ==================== Indexes ====================
        // Primary query pattern: all events for a shipment, ordered by time
        builder.HasIndex("InboundShipmentId", nameof(ShipmentTrackingEvent.EventTime))
            .HasDatabaseName("idx_shipment_tracking_events_inbound_shipment_id");

        builder.HasIndex("OutboundShipmentId", nameof(ShipmentTrackingEvent.EventTime))
            .HasDatabaseName("idx_shipment_tracking_events_outbound_shipment_id");

        // Useful for webhook dedup / audit queries
        builder.HasIndex(e => new { e.ShipmentId, e.CarrierStatusRaw, e.EventTime })
            .HasDatabaseName("idx_shipment_tracking_events_shipment_carrier_status");
    }
}