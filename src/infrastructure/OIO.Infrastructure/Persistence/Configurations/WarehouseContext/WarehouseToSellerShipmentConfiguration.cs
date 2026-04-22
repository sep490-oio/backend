using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseToSellerShipments;
using OIO.Domain.Context.WarehouseContext.Enums;
using OIO.Domain.Context.WarehouseContext.ValueObjects.Ids;

namespace OIO.Infrastructure.Persistence.Configurations.WarehouseContext;

internal sealed class WarehouseToSellerShipmentConfiguration : IEntityTypeConfiguration<WarehouseToSellerShipment>
{
    public void Configure(EntityTypeBuilder<WarehouseToSellerShipment> builder)
    {
        builder.ToTable("warehouse_to_seller_shipments");

        // ==================== Primary Key ====================
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .ValueGeneratedNever()
            .HasColumnName("id")
            .HasConversion(x => x.Value, v => WarehouseToSellerShipmentId.From(v));

        // ==================== Properties ====================
        builder.Property(e => e.WarehouseItemId)
            .HasColumnName("warehouse_item_id")
            .IsRequired()
            .HasConversion(x => x.Value, v => WarehouseItemId.From(v));

        builder.Property(e => e.WarehouseInspectionId)
            .HasColumnName("warehouse_inspection_id")
            .IsRequired()
            .HasConversion(x => x.Value, v => WarehouseInspectionId.From(v));

        builder.Property(e => e.SellerId)
            .HasColumnName("seller_id")
            .IsRequired()
            .HasConversion(x => x.Value, v => UserId.From(v));

        // Snapshot of the seller's default UserAddress at creation (survives later address changes).
        builder.Property(e => e.SellerAddressSnapshot)
            .HasColumnName("seller_address_snapshot")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(e => e.RejectionReason)
            .HasColumnName("rejection_reason")
            .IsRequired();

        builder.Property(e => e.ProviderCode)
            .HasColumnName("provider_code")
            .HasMaxLength(50);

        builder.Property(e => e.TrackingNumber)
            .HasColumnName("tracking_number")
            .HasMaxLength(100);

        builder.Property(e => e.ShippedAt)
            .HasColumnName("shipped_at");

        builder.Property(e => e.DeliveredAt)
            .HasColumnName("delivered_at");

        builder.Property(e => e.SellerConfirmedAt)
            .HasColumnName("seller_confirmed_at");

        builder.Property(e => e.DeliveryFailureReason)
            .HasColumnName("delivery_failure_reason");

        builder.Property(e => e.Status)
            .HasColumnName("status")
            .HasMaxLength(30)
            .IsRequired()
            .HasConversion(x => x.Id, v => WarehouseToSellerShipmentStatus.FromId(v).GetValueOrThrow());

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.Property(e => e.ModifiedAt)
            .HasColumnName("modified_at");

        // Signed return-scoped QR token, issued at MarkShipped time.
        builder.Property(e => e.QrToken)
            .HasColumnName("qr_token")
            .HasMaxLength(512);

        // Evidence — 1-to-many to WarehouseToSellerShipmentEvidence.
        builder.HasMany(e => e.Evidence)
            .WithOne()
            .HasForeignKey(ev => ev.ShipmentId)
            .OnDelete(DeleteBehavior.Cascade);

        // ==================== Indexes ====================
        // Partial unique index: at most one active shipment per inspection.
        // Allows a new row once the prior one reaches a terminal state.
        builder.HasIndex(e => e.WarehouseInspectionId)
            .HasDatabaseName("uq_warehouse_to_seller_shipments_inspection_active")
            .IsUnique()
            .HasFilter("status NOT IN ('closed','returned_to_warehouse')");

        builder.HasIndex(e => new { e.SellerId, e.Status })
            .HasDatabaseName("idx_warehouse_to_seller_shipments_seller_status");

        builder.HasIndex(e => e.WarehouseItemId)
            .HasDatabaseName("idx_warehouse_to_seller_shipments_warehouse_item");

        // ==================== Ignore ====================
        builder.Ignore(e => e.DomainEvents);
    }
}
