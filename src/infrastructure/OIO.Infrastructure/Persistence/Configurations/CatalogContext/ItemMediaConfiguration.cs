using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OIO.Domain.Context.CatalogContext.Aggregates.Items;
using OIO.Domain.Context.CatalogContext.Enums;
using OIO.Domain.Context.CatalogContext.ValueObjects.Ids;

namespace OIO.Infrastructure.Persistence.Configurations.CatalogContext;

internal sealed class ItemMediaConfiguration : IEntityTypeConfiguration<ItemMedia>
{
    public void Configure(EntityTypeBuilder<ItemMedia> builder)
    {
        builder.ToTable("item_media");

        builder.HasKey(im => im.Id);

        builder.Property(im => im.Id)
            .HasColumnName("id")
            .ValueGeneratedNever()
            .IsRequired()
            .HasConversion(
                x => x.Value,
                value => ItemMediaId.From(value));
        
        builder.Property(im => im.ItemId)
            .HasColumnName("item_id")
            .IsRequired()
            .HasConversion(
                x => x.Value,
                value => ItemId.From(value));
        
        builder.Property(im => im.IsPrimary)
            .HasColumnName("is_primary")
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
    }
}