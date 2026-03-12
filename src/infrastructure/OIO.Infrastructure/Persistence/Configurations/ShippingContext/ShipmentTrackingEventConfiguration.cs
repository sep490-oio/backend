using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OIO.Domain.Context.ShippingContext.Aggregates;

namespace OIO.Infrastructure.Persistence.Configurations.ShippingContext;

internal sealed class ShipmentTrackingEventConfiguration : IEntityTypeConfiguration<ShipmentTrackingEvent>
{
    public void Configure(EntityTypeBuilder<ShipmentTrackingEvent> builder)
    {
        builder.ToTable("shipment_tracking_events");

        builder.HasKey(e => e.Id);

        builder.ComplexProperty(e => e.ShipmentType, typeBuilder =>
        {
            typeBuilder.Property(t => t.Id)
                .HasColumnName("shipment_type")
                .HasMaxLength(20)
                .IsRequired();
        });

        builder.Property(e => e.ShipmentId)
            .HasColumnName("shipment_id")
            .IsRequired();

        builder.Property(e => e.ProviderCode)
            .HasColumnName("provider_code")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.CarrierStatusRaw)
            .HasColumnName("carrier_status_raw")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.CarrierStatusDesc)
            .HasColumnName("carrier_status_desc")
            .HasMaxLength(500);

        builder.Property(e => e.NormalizedStatus)
            .HasColumnName("normalized_status")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.Location)
            .HasColumnName("location")
            .HasMaxLength(500);

        builder.Property(e => e.ReasonCode)
            .HasColumnName("reason_code")
            .HasMaxLength(50);

        builder.Property(e => e.ReasonDescription)
            .HasColumnName("reason_description");

        builder.Property(e => e.EventTime)
            .HasColumnName("event_time")
            .IsRequired();

        builder.Property(e => e.RawPayload)
            .HasColumnName("raw_payload")
            .HasColumnType("jsonb")
            .HasDefaultValueSql("'{}'::jsonb")
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        // Indexes
        builder.HasIndex(e => e.ShipmentId)
            .HasDatabaseName("idx_shipment_tracking_events_shipment");

        builder.HasIndex(e => new { e.NormalizedStatus, e.CreatedAt })
            .HasDatabaseName("idx_shipment_tracking_events_status_created");
    }
}
