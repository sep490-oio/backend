using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseStorage;
using OIO.Domain.Context.WarehouseContext.ValueObjects.Ids;

namespace OIO.Infrastructure.Persistence.Configurations.WarehouseContext;

internal sealed class WarehouseStorageLocationConfiguration : IEntityTypeConfiguration<WarehouseStorageLocation>
{
    public void Configure(EntityTypeBuilder<WarehouseStorageLocation> builder)
    {
        builder.ToTable("warehouse_storage_locations");

        // ==================== Primary Key ====================
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .ValueGeneratedNever()
            .HasColumnName("id")
            .HasConversion(x => x.Value, v => WarehouseStorageLocationId.From(v));

        // ==================== Properties ====================
        builder.Property(e => e.Zone)
            .HasColumnName("zone")
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(e => e.Aisle)
            .HasColumnName("aisle")
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(e => e.Shelf)
            .HasColumnName("shelf")
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(e => e.Bin)
            .HasColumnName("bin")
            .HasMaxLength(10)
            .IsRequired();

        // Computed in domain: "{zone}-{aisle}-{shelf}-{bin}" e.g. "A-01-03-02"
        builder.Property(e => e.Label)
            .HasColumnName("label")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.IsOccupied)
            .HasColumnName("is_occupied")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        // ==================== Indexes ====================
        builder.HasIndex(e => e.Label)
            .IsUnique()
            .HasDatabaseName("idx_unique_warehouse_storage_locations_label");

        builder.HasIndex(e => e.IsOccupied)
            .HasDatabaseName("idx_warehouse_storage_locations_vacant")
            .HasFilter("is_occupied = FALSE");

        // ==================== Ignore ====================
        builder.Ignore(e => e.DomainEvents);
    }
}