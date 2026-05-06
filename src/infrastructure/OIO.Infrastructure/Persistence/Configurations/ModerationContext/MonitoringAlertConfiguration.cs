using EFCore.ComplexIndexes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OIO.Domain.Context.ModerationContext.Aggregates;

namespace OIO.Infrastructure.Persistence.Configurations.ModerationContext;

internal sealed class MonitoringAlertConfiguration : IEntityTypeConfiguration<MonitoringAlert>
{
    public void Configure(EntityTypeBuilder<MonitoringAlert> builder)
    {
        builder.ToTable("monitoring_alerts");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.EntityType)
            .HasColumnName("entity_type")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(a => a.EntityId)
            .HasColumnName("entity_id")
            .IsRequired();

        builder.Property(a => a.AlertType)
            .HasColumnName("alert_type")
            .HasMaxLength(50)
            .IsRequired();

        builder.ComplexProperty(a => a.Severity, severityBuilder =>
        {
            severityBuilder.Property(s => s.Id)
                .HasColumnName("severity")
                .HasMaxLength(10)
                .IsRequired();
        });

        builder.Property(a => a.Payload)
            .HasColumnName("payload")
            .HasColumnType("jsonb")
            .HasDefaultValueSql("'{}'::jsonb")
            .IsRequired();

        builder.Property(a => a.Notes)
            .HasColumnName("notes");

        builder.Property(a => a.AcknowledgedBy)
            .HasColumnName("acknowledged_by");

        builder.Property(a => a.AcknowledgedAt)
            .HasColumnName("acknowledged_at");

        builder.Property(a => a.ResolvedBy)
            .HasColumnName("resolved_by");

        builder.Property(a => a.ResolvedAt)
            .HasColumnName("resolved_at");

        builder.Property(a => a.AssignedTo)
            .HasColumnName("assigned_to");

        builder.Property(a => a.AssignedAt)
            .HasColumnName("assigned_at");

        builder.Property(a => a.SlaDueAt)
            .HasColumnName("sla_due_at");

        builder.Property(a => a.ResolutionOutcome)
            .HasColumnName("resolution_outcome")
            .HasMaxLength(50);

        builder.Property(a => a.ResolutionReason)
            .HasColumnName("resolution_reason");

        builder.Property(a => a.Fingerprint)
            .HasColumnName("fingerprint")
            .HasMaxLength(128)
            .IsRequired();

        builder.ComplexProperty(a => a.Status, statusBuilder =>
        {
            statusBuilder.Property(s => s.Id)
                .HasColumnName("status")
                .HasMaxLength(20)
                .IsRequired();
        }).HasComplexCompositeIndex(
            a => new { Status = a.Status.Id, Severity = a.Severity.Id, a.CreatedAt },
            indexName: "idx_monitoring_alerts_status_severity");

        builder.Property(a => a.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        // Indexes
        builder.HasIndex(a => new { a.EntityType, a.EntityId })
            .HasDatabaseName("idx_monitoring_alerts_entity");

        builder.HasIndex(a => new { a.EntityType, a.EntityId, a.AlertType, a.CreatedAt })
            .HasDatabaseName("idx_monitoring_alerts_entity_type_created_at");

        builder.HasIndex(a => a.AssignedTo)
            .HasDatabaseName("idx_monitoring_alerts_assigned_to");

        builder.HasIndex(a => a.Fingerprint)
            .IsUnique()
            .HasFilter("status IN ('open', 'acknowledged')")
            .HasDatabaseName("ux_monitoring_alerts_active_fingerprint");
    }
}
