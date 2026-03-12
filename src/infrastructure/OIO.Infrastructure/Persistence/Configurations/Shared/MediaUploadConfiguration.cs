using EFCore.ComplexIndexes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OIO.Domain.Context.Shared.Entities;
using OIO.Domain.Context.Shared.ValueObjects.Ids;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;

namespace OIO.Infrastructure.Persistence.Configurations.Shared;

internal sealed class MediaUploadConfiguration
    : IEntityTypeConfiguration<MediaUpload>
{
    public void Configure(EntityTypeBuilder<MediaUpload> builder)
    {
        builder.ToTable("media_uploads");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasColumnName("id")
            .ValueGeneratedNever()
            .HasConversion(x => x.Value, x => MediaUploadId.From(x));

        builder.Property(p => p.UserId)
            .HasColumnName("user_id")
            .IsRequired()
            .HasConversion(x => x.Value, x => UserId.From(x));

        builder.Property(u => u.Context)
            .HasColumnName("context")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(u => u.ResourceType)
            .HasColumnName("resource_type")
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(u => u.EntityId)
            .HasColumnName("entity_id");

        builder.Property(u => u.IsConfirmed)
            .HasColumnName("is_confirmed")
            .HasDefaultValue(false);

        builder.Property(u => u.IsLinked)
            .HasColumnName("is_linked")
            .HasDefaultValue(false);

        builder.Property(u => u.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.Property(u => u.ExpiresAt)
            .HasColumnName("expires_at")
            .IsRequired();

        builder.Property(u => u.ConfirmedAt)
            .HasColumnName("confirmed_at");

        builder.Property(u => u.LinkedAt)
            .HasColumnName("linked_at");

        builder.ComplexProperty(c => c.StorageRef, storageRefBuilder =>
        {
            storageRefBuilder.Property(s => s.PublicId)
                .HasColumnName("public_id")
                .HasComplexIndex(indexName: "idx_media_uploads_public_id");
            
            storageRefBuilder.Property(s => s.Folder)
                .HasColumnName("folder");
        });

        builder.ComplexProperty(c => c.Info, iconInfoBuilder =>
        {
            iconInfoBuilder.Property(i => i.SecureUrl)
                .HasColumnName("secure_url");
            
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

        // Indexes
        builder.HasIndex(u => u.UserId)
            .HasDatabaseName("idx_media_uploads_user_id");

        builder.HasIndex(u => new { u.IsConfirmed, u.ExpiresAt })
            .HasDatabaseName("idx_media_uploads_expired")
            .HasFilter("is_confirmed = false");

        builder.HasIndex(u => new { u.IsConfirmed, u.IsLinked, u.ConfirmedAt })
            .HasDatabaseName("idx_media_uploads_orphan")
            .HasFilter("is_confirmed = true AND is_linked = false");


    }
}