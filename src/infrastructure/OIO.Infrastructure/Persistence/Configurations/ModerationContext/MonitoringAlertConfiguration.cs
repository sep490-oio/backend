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
    }
}
