using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseItems;
using OIO.Domain.Context.WarehouseContext.ValueObjects.Ids;

namespace OIO.Infrastructure.Persistence.Configurations.WarehouseContext;

internal sealed class WarehouseItemMediaConfiguration : IEntityTypeConfiguration<WarehouseItemMedia>
{
    public void Configure(EntityTypeBuilder<WarehouseItemMedia> builder)
    {
        builder.ToTable("warehouse_item_media");

        builder.HasKey(im => im.Id);

        builder.Property(im => im.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd()
            .IsRequired()
            .HasConversion(
                x => x.Value,
                value => WarehouseItemMediaId.From(value));
        
        builder.Property(im => im.WarehouseItemId)
            .HasColumnName("warehouse_item_id")
            .IsRequired()
            .HasConversion(
                x => x.Value,
                value => WarehouseItemId.From(value));
        
        builder.Property(im => im.IsPrimary)
            .HasColumnName("is_primary")
            .IsRequired();
            
        builder.Property(im => im.ResourceType)
            .HasColumnName("resource_type")
            .HasMaxLength(20)
            .IsRequired();
        
        builder.Property(im => im.SortOrder)
            .HasColumnName("sort_order")
            .IsRequired();
        
        builder.ComplexProperty(c => c.StorageRef, storageRefBuilder =>
        {
            storageRefBuilder.Property(s => s.PublicId)
                .HasColumnName("public_id");
            
            storageRefBuilder.Property(s => s.Folder)
                .HasColumnName("folder");
        });

        builder.ComplexProperty(c => c.Info, iconInfoBuilder =>
        {
            iconInfoBuilder.Property(i => i.SecureUrl)
                .HasColumnName("secure_url")
                .IsRequired();
            
            iconInfoBuilder.Property(i => i.FileName)
                .HasColumnName("file_name");
            
            iconInfoBuilder.Property(i => i.Bytes)
                .HasColumnName("bytes");
            
            iconInfoBuilder.Property(i => i.Format)
                .HasColumnName("format");
            
            iconInfoBuilder.Property(i => i.Width)
                .HasColumnName("width");
            
            iconInfoBuilder.Property(i => i.Height)
                .HasColumnName("height");
            
            iconInfoBuilder.Property(i => i.DurationSeconds)
                .HasColumnName("duration_seconds");

            iconInfoBuilder.Ignore(i => i.IsVideo);
            iconInfoBuilder.Ignore(i => i.IsImage);
        });
        
        builder.Property(im => im.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();
            
        
        builder.HasIndex(e => e.WarehouseItemId)
            .HasDatabaseName("idx_warehouse_item_media_warehouse_item_id");
    }
}
