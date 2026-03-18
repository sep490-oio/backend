using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OIO.Domain.Context.UserContext.Aggregates.Users;

namespace OIO.Infrastructure.Persistence.Configurations.UserContext;

internal sealed class TermsDocumentConfiguration : IEntityTypeConfiguration<TermsDocument>
{
    public void Configure(EntityTypeBuilder<TermsDocument> builder)
    {
        builder.ToTable("terms_documents");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.TermType)
            .HasColumnName("term_type")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(t => t.Version)
            .HasColumnName("version")
            .IsRequired();

        builder.ComplexProperty(t => t.StorageRef, sr =>
        {
            sr.Property(x => x.PublicId)
                .HasColumnName("storage_public_id");

            sr.Property(x => x.Folder)
                .HasColumnName("storage_folder");
        });

        builder.ComplexProperty(t => t.Info, info =>
        {
            info.Property(x => x.SecureUrl)
                .HasColumnName("content_url")
                .HasMaxLength(500)
                .IsRequired();

            info.Property(x => x.FileName)
                .HasColumnName("file_name");

            info.Property(x => x.Bytes)
                .HasColumnName("file_size");

            info.Property(x => x.Format)
                .HasColumnName("format");

            info.Property(x => x.Width)
                .HasColumnName("width");

            info.Property(x => x.Height)
                .HasColumnName("height");

            info.Property(x => x.DurationSeconds)
                .HasColumnName("duration_seconds");

            info.Ignore(x => x.IsVideo);
            info.Ignore(x => x.IsImage);
        });

        builder.Property(t => t.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(false);

        builder.Property(t => t.PublishedAt)
            .HasColumnName("published_at");

        builder.Property(t => t.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        // Constraints
        builder.HasIndex(t => new { t.TermType, t.Version })
            .IsUnique()
            .HasDatabaseName("uq_terms_documents_type_version");

        // Indexes
        builder.HasIndex(t => t.TermType)
            .HasDatabaseName("idx_terms_documents_type");

        builder.HasIndex(t => t.PublishedAt)
            .HasDatabaseName("idx_terms_documents_published_at");
    }
}
