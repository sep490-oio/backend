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

        builder.Property(u => u.PublicId)
            .HasColumnName("public_id")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(u => u.Folder)
            .HasColumnName("folder")
            .HasMaxLength(500)
            .IsRequired();

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

        builder.Property(u => u.SecureUrl)
            .HasColumnName("secure_url")
            .HasMaxLength(500);

        builder.Property(u => u.FileName)
            .HasColumnName("file_name")
            .HasMaxLength(255);

        builder.Property(u => u.Bytes)
            .HasColumnName("bytes");

        builder.Property(u => u.Format)
            .HasColumnName("format")
            .HasMaxLength(20);

        builder.Property(u => u.Width)
            .HasColumnName("width");

        builder.Property(u => u.Height)
            .HasColumnName("height");

        builder.Property(u => u.DurationSeconds)
            .HasColumnName("duration_seconds");

        // Indexes
        builder.HasIndex(u => u.UserId)
            .HasDatabaseName("idx_media_uploads_user_id");

        builder.HasIndex(u => new { u.IsConfirmed, u.ExpiresAt })
            .HasDatabaseName("idx_media_uploads_expired")
            .HasFilter("is_confirmed = false");

        builder.HasIndex(u => new { u.IsConfirmed, u.IsLinked, u.ConfirmedAt })
            .HasDatabaseName("idx_media_uploads_orphan")
            .HasFilter("is_confirmed = true AND is_linked = false");

        builder.HasIndex(u => u.PublicId)
            .HasDatabaseName("idx_media_uploads_public_id");

    }
}

internal sealed class SystemSettingConfiguration
    : IEntityTypeConfiguration<SystemSetting>
{
    public void Configure(EntityTypeBuilder<SystemSetting> builder)
    {
        builder.ToTable("system_settings");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .HasColumnName("Id")
            .HasMaxLength(200)
            .IsRequired()
            .HasConversion(x => x.Value, x => SystemSettingId.From(x));

        builder.Property(s => s.Value)
            .HasColumnName("value")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(s => s.Description)
            .HasColumnName("description")
            .HasMaxLength(500);

        builder.Property(s => s.ValueType)
            .HasColumnName("value_type")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(s => s.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(s => s.ModifiedAt)
            .HasColumnName("modified_at");

        builder.Property(s => s.ModifiedBy)
            .HasColumnName("modified_by")
            .HasMaxLength(100);
    }
}