using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OIO.Domain.Context.CatalogContext.Aggregates.Items;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.Context.CatalogContext.ValueObjects.Ids;
using OIO.Domain.Context.WarehouseContext.Aggregates.InboundShipments;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseItems;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseStorage;
using OIO.Domain.Context.WarehouseContext.Enums;
using OIO.Domain.Context.WarehouseContext.ValueObjects.Ids;

namespace OIO.Infrastructure.Persistence.Configurations.WarehouseContext;

internal sealed class WarehouseItemConfiguration : IEntityTypeConfiguration<WarehouseItem>
{
    public void Configure(EntityTypeBuilder<WarehouseItem> builder)
    {
        builder.ToTable("warehouse_items");

        // ==================== Primary Key ====================
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .ValueGeneratedNever()
            .HasColumnName("id")
            .HasConversion(x => x.Value, v => WarehouseItemId.From(v));

        // ==================== Properties ====================
        // Soft FK — no EF navigation to items table
        builder.Property(e => e.ItemId)
            .HasColumnName("item_id")
            .IsRequired();

        builder.Property(e => e.InboundShipmentId)
            .HasColumnName("inbound_shipment_id")
            .IsRequired()
            .HasConversion(x => x.Value, v => InboundShipmentId.From(v));

        builder.Property(e => e.StorageLocationId)
            .HasColumnName("storage_location_id")
            .HasConversion(
                x => x.HasValue ? x.Value.Value : (Guid?)null,
                v => v.HasValue ? WarehouseStorageLocationId.From(v.Value) : null);

        var navigation = builder.Metadata.FindNavigation(nameof(WarehouseItem.Media))!;
        navigation.SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.Property(e => e.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .IsRequired()
            .HasConversion(x => x.Id, v => WarehouseItemStatus.FromId(v).GetValueOrThrow());

        builder.Property(e => e.ReceivedAt)
            .HasColumnName("received_at");

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.Property(e => e.ModifiedAt)
            .HasColumnName("modified_at");

        // ==================== Relationships ====================
        // EF FK to inbound_shipments (1-to-1)
        builder.HasOne<InboundShipment>()
            .WithOne()
            .HasForeignKey<WarehouseItem>(e => e.InboundShipmentId)
            .OnDelete(DeleteBehavior.Restrict);

        // EF FK to warehouse_storage_locations (many-to-1, nullable)
        builder.HasOne<WarehouseStorageLocation>()
            .WithMany()
            .HasForeignKey(e => e.StorageLocationId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<Item>()
            .WithMany()
            .HasForeignKey("item_id")
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey("inspected_by")
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.Media)
            .WithOne(m => m.WarehouseItem)
            .HasForeignKey(m => m.WarehouseItemId)
            .OnDelete(DeleteBehavior.Cascade);

        // ==================== Indexes ====================
        builder.HasIndex(e => e.InboundShipmentId)
            .IsUnique()
            .HasDatabaseName("idx_unique_warehouse_items_inbound_shipment_id");

        builder.HasIndex(e => e.ItemId)
            .HasDatabaseName("idx_warehouse_items_item_id");

        builder.HasIndex(e => e.StorageLocationId)
            .HasDatabaseName("idx_warehouse_items_storage_location_id")
            .HasFilter("storage_location_id IS NOT NULL");

        builder.HasIndex(e => e.Status)
            .HasDatabaseName("idx_warehouse_items_status");

        // ==================== Ignore ====================
        builder.Ignore(e => e.DomainEvents);
    }
}
