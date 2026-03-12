using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.Enums;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;

namespace OIO.Infrastructure.Persistence.Configurations.UserContext;

public class VerificationDocumentConfiguration : IEntityTypeConfiguration<VerificationDocument>
{
    public void Configure(EntityTypeBuilder<VerificationDocument> builder)
    {
        builder.ToTable("user_identity_verification_documents");
        
        builder.HasKey(f => f.Id);
        
        builder.Property(f => f.Id)
            .ValueGeneratedNever()
            .HasColumnName("id")
            .HasConversion(x => x.Value, value => VerificationDocumentId.From(value));

        builder.Property(f => f.VerificationId)
            .HasColumnName("verification_id")
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
        
        builder.Property(f => f.FileHash)
            .HasMaxLength(64);
        
        builder.Property(f => f.MimeType)
            .HasMaxLength(50);
        
        builder.ComplexProperty(x => x.VerificationStatus, verificationStatusBuilder =>
        {
            verificationStatusBuilder.Property(vs => vs.Id)
                .HasColumnName("verification_status")
                .HasMaxLength(20)
                .HasDefaultValue(DocumentVerificationStatus.Pending.Id)
                .IsRequired();
        });

        builder.Property(f => f.VerificationNotes)
            .HasColumnName("verification_notes");
        
        builder.Property(f => f.ExtractedData)
            .HasColumnType("jsonb")
            .HasColumnName("extracted_data");
        
        builder.Property(f => f.UploadedAt)
            .HasColumnName("uploaded_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();
        
        builder.Property(f => f.VerifiedAt)
            .HasColumnName("verified_at");

        builder.Property(f => f.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();
    }
}