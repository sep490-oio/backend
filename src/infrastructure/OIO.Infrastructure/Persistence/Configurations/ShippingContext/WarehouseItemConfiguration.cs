using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OIO.Domain.Context.ShippingContext.Aggregates;

namespace OIO.Infrastructure.Persistence.Configurations.ShippingContext;

internal sealed class WarehouseItemConfiguration : IEntityTypeConfiguration<WarehouseItem>
{
    public void Configure(EntityTypeBuilder<WarehouseItem> builder)
    {
        builder.ToTable("warehouse_items");

        builder.HasKey(w => w.Id);

        builder.Property(w => w.ItemId)
            .HasColumnName("item_id")
            .IsRequired();

        builder.Property(w => w.InboundShipmentId)
            .HasColumnName("inbound_shipment_id")
            .IsRequired();

        builder.Property(w => w.StorageLocationId)
            .HasColumnName("storage_location_id");

        builder.ComplexProperty(w => w.ConditionOnArrival, condBuilder =>
        {
            condBuilder.Property(c => c.Id)
                .HasColumnName("condition_on_arrival")
                .HasMaxLength(20)
                .IsRequired();
        });

        builder.Property(w => w.InspectionNotes)
            .HasColumnName("inspection_notes");

        builder.Property(w => w.InspectionImages)
            .HasColumnName("inspection_images")
            .HasColumnType("jsonb");

        builder.ComplexProperty(w => w.Status, statusBuilder =>
        {
            statusBuilder.Property(s => s.Id)
                .HasColumnName("status")
                .HasMaxLength(20)
                .IsRequired();
        });

        builder.Property(w => w.InspectedBy)
            .HasColumnName("inspected_by");

        builder.Property(w => w.InspectedAt)
            .HasColumnName("inspected_at");

        builder.Property(w => w.ReceivedAt)
            .HasColumnName("received_at");

        builder.Property(w => w.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.Property(w => w.ModifiedAt)
            .HasColumnName("modified_at");

        // Navigation
        builder.HasOne(w => w.StorageLocation)
            .WithMany()
            .HasForeignKey(w => w.StorageLocationId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(w => w.OutboundShipment)
            .WithOne(o => o.WarehouseItem)
            .HasForeignKey<OutboundShipment>(o => o.WarehouseItemId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasOne(w => w.Item)
            .WithOne(i => i.WarehouseItem)
            .HasForeignKey<WarehouseItem>(w => w.ItemId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(w => w.ItemId)
            .HasDatabaseName("idx_warehouse_items_item");

        builder.HasIndex(w => w.InboundShipmentId)
            .HasDatabaseName("idx_warehouse_items_inbound_shipment");

        builder.HasIndex(w => w.StorageLocationId)
            .HasDatabaseName("idx_warehouse_items_storage_location");
    }
}
