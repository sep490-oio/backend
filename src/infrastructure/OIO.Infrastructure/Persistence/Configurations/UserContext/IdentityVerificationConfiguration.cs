using EFCore.ComplexIndexes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OIO.Domain.Context.UserContext.Aggregates.Users;

namespace OIO.Infrastructure.Persistence.Configurations.UserContext;

internal sealed class IdentityVerificationConfiguration : IEntityTypeConfiguration<IdentityVerification>
{
    public void Configure(EntityTypeBuilder<IdentityVerification> builder)
    {
        builder.ToTable("user_identity_verifications");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.ComplexProperty(v => v.VerificationType, vtBuilder =>
        {
            vtBuilder.Property(x => x.Id)
                .HasColumnName("verification_type")
                .HasMaxLength(30)
                .IsRequired();
        });

        builder.Property(v => v.FullName)
            .HasColumnName("full_name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(v => v.DateOfBirth)
            .HasColumnName("date_of_birth");

        builder.ComplexProperty(v => v.Gender, genderBuilder =>
        {
            genderBuilder.Property(g => g.Id)
                .HasColumnName("gender")
                .HasMaxLength(10);
        });

        builder.Property(v => v.Nationality)
            .HasColumnName("nationality")
            .HasMaxLength(100)
            .HasDefaultValue("Việt Nam");

        builder.ComplexProperty(v => v.Document, doc =>
        {
            doc.ComplexProperty(d => d.IdType, idTypeBuilder =>
            {
                idTypeBuilder.Property(x => x.Id)
                    .HasColumnName("id_type")
                    .HasMaxLength(20)
                    .IsRequired();
            });

            doc.Property(d => d.IdNumber)
                .HasColumnName("id_number")
                .HasMaxLength(50)
                .IsRequired();

            doc.Property(d => d.IssuedDate)
                .HasColumnName("id_issued_date");

            doc.Property(d => d.ExpiredDate)
                .HasColumnName("id_expired_date");

            doc.Property(d => d.IssuedPlace)
                .HasColumnName("id_issued_place")
                .HasMaxLength(200);
        });

        builder.ComplexProperty(v => v.PermanentAddress, pa =>
        {
            pa.Property(a => a.FullAddress)
                .HasColumnName("permanent_address");

            pa.Property(a => a.Province)
                .HasColumnName("province")
                .HasMaxLength(100);

            pa.Property(a => a.District)
                .HasColumnName("district")
                .HasMaxLength(100);

            pa.Property(a => a.Ward)
                .HasColumnName("ward")
                .HasMaxLength(100);
        });

        builder.ComplexProperty(v => v.Status, statusBuilder =>
        {
            statusBuilder.Property(s => s.Id)
                .HasColumnName("status")
                .HasMaxLength(20)
                .IsRequired()
                .HasComplexIndex(indexName: "idx_user_identity_verifications_status");
        });

        builder.Property(v => v.VerifiedAt)
            .HasColumnName("verified_at");

        builder.Property(v => v.VerifiedBy)
            .HasColumnName("verified_by");

        builder.Property(v => v.RejectionReason)
            .HasColumnName("rejection_reason");

        builder.Property(v => v.RejectionCode)
            .HasColumnName("rejection_code")
            .HasMaxLength(50);

        builder.Property(v => v.AutoVerified)
            .HasColumnName("auto_verified")
            .HasDefaultValue(false);

        builder.Property(v => v.AutoVerifyScore)
            .HasColumnName("auto_verify_score")
            .HasColumnType("numeric(5,2)");

        builder.Property(v => v.AutoVerifyProvider)
            .HasColumnName("auto_verify_provider")
            .HasMaxLength(50);

        builder.Property(v => v.AutoVerifyResponse)
            .HasColumnName("auto_verify_response")
            .HasColumnType("jsonb");

        builder.Property(v => v.SubmittedAt)
            .HasColumnName("submitted_at");

        builder.Property(v => v.ExpiresAt)
            .HasColumnName("expires_at");

        builder.Property(v => v.AttemptCount)
            .HasColumnName("attempt_count")
            .HasDefaultValue(0);

        builder.Property(v => v.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.Property(v => v.ModifiedAt)
            .HasColumnName("modified_at");

        // Navigation
        builder.HasMany(v => v.Documents)
            .WithOne(d => d.Verification)
            .HasForeignKey(d => d.VerificationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(v => v.History)
            .WithOne(h => h.Verification)
            .HasForeignKey(h => h.VerificationId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(v => v.UserId)
            .HasDatabaseName("idx_user_identity_verifications_user");

        builder.HasIndex(v => v.SubmittedAt)
            .HasDatabaseName("idx_user_identity_verifications_submitted")
            .HasFilter("status = 'submitted'");
    }
}
