using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OIO.Domain.Context.ShippingContext.Aggregates;

namespace OIO.Infrastructure.Persistence.Configurations.ShippingContext;

internal sealed class WarehouseStorageLocationConfiguration : IEntityTypeConfiguration<WarehouseStorageLocation>
{
    public void Configure(EntityTypeBuilder<WarehouseStorageLocation> builder)
    {
        builder.ToTable("warehouse_storage_locations");

        builder.HasKey(l => l.Id);

        builder.ComplexProperty(l => l.Location, loc =>
        {
            loc.Property(x => x.Zone)
                .HasColumnName("zone")
                .HasMaxLength(10)
                .IsRequired();

            loc.Property(x => x.Aisle)
                .HasColumnName("aisle")
                .HasMaxLength(10)
                .IsRequired();

            loc.Property(x => x.Shelf)
                .HasColumnName("shelf")
                .HasMaxLength(10)
                .IsRequired();

            loc.Property(x => x.Bin)
                .HasColumnName("bin")
                .HasMaxLength(10)
                .IsRequired();

            loc.Property(x => x.Label)
                .HasColumnName("label")
                .HasMaxLength(50)
                .IsRequired();
        });

        builder.Property(l => l.IsOccupied)
            .HasColumnName("is_occupied")
            .HasDefaultValue(false);

        builder.Property(l => l.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();
    }
}
