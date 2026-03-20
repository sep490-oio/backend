using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OIO.Domain.Context.ModerationContext.Aggregates.Disputes;
using OIO.Domain.Context.ModerationContext.ValueObjects.Ids;
using OIO.Domain.Context.Shared.ValueObjects.Ids;

namespace OIO.Infrastructure.Persistence.Configurations.ModerationContext;

internal sealed class DisputeMessageAttachmentConfiguration : IEntityTypeConfiguration<DisputeMessageAttachment>
{
    public void Configure(EntityTypeBuilder<DisputeMessageAttachment> builder)
    {
        builder.ToTable("dispute_message_attachments");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.DisputeId)
            .HasColumnName("dispute_id")
            .IsRequired()
            .HasConversion(x => x.Value, v => DisputeId.From(v));

        builder.Property(x => x.DisputeMessageId)
            .HasColumnName("dispute_message_id")
            .IsRequired()
            .HasConversion(x => x.Value, v => DisputeMessageId.From(v));

        builder.Property(x => x.MediaUploadId)
            .HasColumnName("media_upload_id")
            .IsRequired()
            .HasConversion(x => x.Value, v => MediaUploadId.From(v));

        builder.Property(x => x.SortOrder)
            .HasColumnName("sort_order")
            .HasDefaultValue(0)
            .IsRequired();

        builder.ComplexProperty(x => x.StorageRef, storageBuilder =>
        {
            storageBuilder.Property(s => s.PublicId)
                .HasColumnName("public_id")
                .IsRequired();

            storageBuilder.Property(s => s.Folder)
                .HasColumnName("folder")
                .IsRequired();
        });

        builder.ComplexProperty(x => x.Info, infoBuilder =>
        {
            infoBuilder.Property(i => i.SecureUrl)
                .HasColumnName("secure_url")
                .IsRequired();

            infoBuilder.Property(i => i.FileName)
                .HasColumnName("file_name");

            infoBuilder.Property(i => i.Bytes)
                .HasColumnName("bytes");

            infoBuilder.Property(i => i.Format)
                .HasColumnName("format");

            infoBuilder.Property(i => i.Width)
                .HasColumnName("width");

            infoBuilder.Property(i => i.Height)
                .HasColumnName("height");

            infoBuilder.Property(i => i.DurationSeconds)
                .HasColumnName("duration_seconds");

            infoBuilder.Ignore(i => i.IsVideo);
            infoBuilder.Ignore(i => i.IsImage);
        });

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.Property(x => x.ModifiedAt)
            .HasColumnName("modified_at");

        builder.HasOne(x => x.Dispute)
            .WithMany()
            .HasForeignKey(x => x.DisputeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.DisputeMessage)
            .WithMany(x => x.Attachments)
            .HasForeignKey(x => x.DisputeMessageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.DisputeMessageId)
            .HasDatabaseName("idx_dispute_message_attachments_message_id");

        builder.HasIndex(x => x.MediaUploadId)
            .IsUnique()
            .HasDatabaseName("idx_unique_dispute_message_attachments_media_upload_id");
    }
}
