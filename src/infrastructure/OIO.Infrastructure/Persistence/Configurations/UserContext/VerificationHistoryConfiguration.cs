using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OIO.Domain.Context.UserContext.Aggregates.Users;

namespace OIO.Infrastructure.Persistence.Configurations.UserContext;

internal sealed class VerificationHistoryConfiguration : IEntityTypeConfiguration<VerificationHistory>
{
    public void Configure(EntityTypeBuilder<VerificationHistory> builder)
    {
        builder.ToTable("user_identity_verification_history");

        builder.HasKey(h => h.Id);

        builder.Property(h => h.VerificationId)
            .HasColumnName("verification_id")
            .IsRequired();

        builder.ComplexProperty(h => h.Action, actionBuilder =>
        {
            actionBuilder.Property(a => a.Id)
                .HasColumnName("action")
                .HasMaxLength(30)
                .IsRequired();
        });

        builder.Property(h => h.OldStatus)
            .HasColumnName("old_status")
            .HasMaxLength(20);

        builder.Property(h => h.NewStatus)
            .HasColumnName("new_status")
            .HasMaxLength(20);

        builder.Property(h => h.ChangedFields)
            .HasColumnName("changed_fields")
            .HasColumnType("jsonb");

        builder.Property(h => h.Notes)
            .HasColumnName("notes");

        builder.Property(h => h.PerformedBy)
            .HasColumnName("performed_by");

        builder.ComplexProperty(h => h.PerformedByType, pbtBuilder =>
        {
            pbtBuilder.Property(p => p.Id)
                .HasColumnName("performed_by_type")
                .HasMaxLength(20);
        });

        builder.Property(h => h.IpAddress)
            .HasColumnName("ip_address")
            .HasColumnType("inet");

        builder.Property(h => h.UserAgent)
            .HasColumnName("user_agent");

        builder.Property(h => h.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        // Indexes
        builder.HasIndex(h => h.VerificationId)
            .HasDatabaseName("idx_user_identity_verification_history_verification");

        builder.HasIndex(h => h.CreatedAt)
            .HasDatabaseName("idx_user_identity_verification_history_created");
    }
}
