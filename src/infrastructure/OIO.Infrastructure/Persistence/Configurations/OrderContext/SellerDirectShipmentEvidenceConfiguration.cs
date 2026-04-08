using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OIO.Domain.Context.OrderContext.Aggregates.SellerDirectShipments;
using OIO.Infrastructure.Persistence.Converters;

namespace OIO.Infrastructure.Persistence.Configurations.OrderContext;

internal sealed class SellerDirectShipmentEvidenceConfiguration : IEntityTypeConfiguration<SellerDirectShipmentEvidence>
{
    public void Configure(EntityTypeBuilder<SellerDirectShipmentEvidence> builder)
    {
        builder.ToTable("seller_direct_shipment_evidence");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .ValueGeneratedNever()
            .HasColumnName("id");

        builder.Property(e => e.ShipmentId)
            .HasColumnName("shipment_id")
            .IsRequired();

        builder.Property(e => e.Kind)
            .HasColumnName("kind")
            .HasEnumConversion(40)
            .IsRequired();

        builder.Property(e => e.MediaUploadId)
            .HasColumnName("media_upload_id")
            .IsRequired();

        builder.Property(e => e.MediaUrl)
            .HasColumnName("media_url")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(e => e.CreatedByUserId)
            .HasColumnName("created_by_user_id")
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.HasOne<SellerDirectShipment>()
            .WithMany(s => s.Evidence)
            .HasForeignKey(e => e.ShipmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.ShipmentId, e.Kind })
            .HasDatabaseName("ix_seller_direct_shipment_evidence_shipment_id_kind");
    }
}
