using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OIO.Domain.Context.AuctionContext.Aggregates.Items;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;

namespace OIO.Infrastructure.Persistence.Configurations.AuctionContext;

internal sealed class ItemMediaConfiguration : IEntityTypeConfiguration<ItemMedia>
{
    public void Configure(EntityTypeBuilder<ItemMedia> builder)
    {
        builder.ToTable("item_media");

        builder.HasKey(img => img.Id);

        builder.Property(img => img.Id)
            .HasColumnName("id")
            .ValueGeneratedNever()
            .HasConversion(x => x.Value, x => ItemMediaId.From(x));

        builder.Property(img => img.ItemId)
            .HasColumnName("item_id")
            .IsRequired()
            .HasConversion(x => x.Value, x => ItemId.From(x));

        builder.Property(m => m.Url)
            .HasColumnName("url")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(m => m.PublicId)
            .HasColumnName("public_id")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(m => m.ResourceType)
            .HasColumnName("resource_type")
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(m => m.IsPrimary)
            .HasColumnName("is_primary")
            .HasDefaultValue(false);

        builder.Property(m => m.SortOrder)
            .HasColumnName("sort_order")
            .HasDefaultValue(0);

        builder.Property(m => m.FileName)
            .HasColumnName("file_name")
            .HasMaxLength(255);

        builder.Property(m => m.Bytes)
            .HasColumnName("bytes");

        builder.Property(m => m.Format)
            .HasColumnName("format")
            .HasMaxLength(20);

        builder.Property(m => m.Width)
            .HasColumnName("width");

        builder.Property(m => m.Height)
            .HasColumnName("height");

        builder.Property(m => m.DurationSeconds)
            .HasColumnName("duration_seconds");

        builder.Property(m => m.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        // Indexes
        builder.HasIndex(m => m.PublicId)
            .HasDatabaseName("idx_item_media_public_id");

        builder.HasIndex(m => new { m.ItemId, m.ResourceType })
            .HasDatabaseName("idx_item_media_item_type");

        // Ignore computed properties
        builder.Ignore(m => m.IsImage);
        builder.Ignore(m => m.IsVideo);
        builder.Ignore(m => m.IsDocument);
    }
}