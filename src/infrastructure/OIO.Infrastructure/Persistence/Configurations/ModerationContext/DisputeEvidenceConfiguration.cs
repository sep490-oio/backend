using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OIO.Domain.Context.ModerationContext.Aggregates.Disputes;

namespace OIO.Infrastructure.Persistence.Configurations.ModerationContext;

internal sealed class DisputeEvidenceConfiguration : IEntityTypeConfiguration<DisputeEvidence>
{
    public void Configure(EntityTypeBuilder<DisputeEvidence> builder)
    {
        builder.ToTable("dispute_evidences");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.DisputeId)
            .HasColumnName("dispute_id")
            .IsRequired();

        builder.Property(e => e.SubmittedBy)
            .HasColumnName("submitted_by")
            .IsRequired();

        builder.ComplexProperty(e => e.Type, typeBuilder =>
        {
            typeBuilder.Property(t => t.Id)
                .HasColumnName("type")
                .HasMaxLength(30)
                .IsRequired();
        });

        builder.ComplexProperty(c => c.EvidenceStorage, iconStorageRefBuilder =>
        {
            iconStorageRefBuilder.Property(s => s.PublicId)
                .HasColumnName("evidence_public_id");
            
            iconStorageRefBuilder.Property(s => s.Folder)
                .HasColumnName("evidence_folder");
        });

        builder.ComplexProperty(c => c.EvidenceInfo, iconInfoBuilder =>
        {
            iconInfoBuilder.Property(i => i.SecureUrl)
                .HasColumnName("evidence_secure_url")
                .IsRequired();
            
            iconInfoBuilder.Property(i => i.FileName)
                .HasColumnName("evidence_file_name");
            
            iconInfoBuilder.Property(i => i.Bytes)
                .HasColumnName("evidence_bytes");
            
            iconInfoBuilder.Property(i => i.Format)
                .HasColumnName("evidence_format");
            
            iconInfoBuilder.Property(i => i.Width)
                .HasColumnName("evidence_width");
            
            iconInfoBuilder.Property(i => i.Height)
                .HasColumnName("evidence_height");
            
            iconInfoBuilder.Property(i => i.DurationSeconds)
                .HasColumnName("evidence_duration_seconds");

            iconInfoBuilder.Ignore(i => i.IsVideo);
            iconInfoBuilder.Ignore(i => i.IsImage);
        });

        builder.Property(e => e.Description)
            .HasColumnName("description");

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();
    }
}
