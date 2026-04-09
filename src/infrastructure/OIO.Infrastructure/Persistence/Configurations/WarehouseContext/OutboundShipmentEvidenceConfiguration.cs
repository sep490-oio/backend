using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OIO.Domain.Context.Shared.ValueObjects.Ids;
using OIO.Domain.Context.WarehouseContext.Aggregates.OutboundShipments;
using OIO.Domain.Context.WarehouseContext.ValueObjects.Ids;

namespace OIO.Infrastructure.Persistence.Configurations.WarehouseContext;

internal sealed class OutboundShipmentEvidenceConfiguration
    : IEntityTypeConfiguration<OutboundShipmentEvidence>
{
    public void Configure(EntityTypeBuilder<OutboundShipmentEvidence> builder)
    {
        builder.ToTable("outbound_shipment_evidence");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .ValueGeneratedNever()
            .HasColumnName("id")
            .HasConversion(x => x.Value, v => OutboundShipmentEvidenceId.From(v));

        builder.Property(e => e.ShipmentId)
            .HasColumnName("shipment_id")
            .IsRequired()
            .HasConversion(x => x.Value, v => OutboundShipmentId.From(v));

        builder.Property(e => e.Category)
            .HasColumnName("category")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.MediaUploadId)
            .HasColumnName("media_upload_id")
            .IsRequired()
            .HasConversion(x => x.Value, v => MediaUploadId.From(v));

        builder.Property(e => e.SecureUrl)
            .HasColumnName("secure_url")
            .HasMaxLength(1000);

        builder.Property(e => e.FileName)
            .HasColumnName("file_name")
            .HasMaxLength(500);

        builder.Property(e => e.ResourceType)
            .HasColumnName("resource_type")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        // Indexes
        builder.HasIndex(e => e.ShipmentId)
            .HasDatabaseName("idx_outbound_shipment_evidence_shipment_id");

        builder.HasIndex(e => new { e.ShipmentId, e.Category })
            .HasDatabaseName("idx_outbound_shipment_evidence_shipment_category");
    }
}
