using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OIO.Domain.Context.ModerationContext.Aggregates;

namespace OIO.Infrastructure.Persistence.Configurations.ModerationContext;

internal sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.ActorUserId)
            .HasColumnName("actor_user_id");

        builder.Property(a => a.ActorRole)
            .HasColumnName("actor_role")
            .HasMaxLength(30);

        builder.Property(a => a.Action)
            .HasColumnName("action")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(a => a.EntityType)
            .HasColumnName("entity_type")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(a => a.EntityId)
            .HasColumnName("entity_id");

        builder.Property(a => a.OldData)
            .HasColumnName("old_data")
            .HasColumnType("jsonb");

        builder.Property(a => a.NewData)
            .HasColumnName("new_data")
            .HasColumnType("jsonb");

        builder.Property(a => a.IpAddress)
            .HasColumnName("ip_address")
            .HasColumnType("inet");

        builder.Property(a => a.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        // Indexes
        builder.HasIndex(a => new { a.ActorUserId, a.CreatedAt })
            .IsDescending(false, true)
            .HasDatabaseName("idx_audit_logs_actor");

        builder.HasIndex(a => new { a.EntityType, a.EntityId, a.CreatedAt })
            .IsDescending(false, false, true)
            .HasDatabaseName("idx_audit_logs_entity");

        builder.HasIndex(a => new { a.Action, a.CreatedAt })
            .IsDescending(false, true)
            .HasDatabaseName("idx_audit_logs_action");
    }
}
