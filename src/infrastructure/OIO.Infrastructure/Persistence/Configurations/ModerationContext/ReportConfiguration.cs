using EFCore.ComplexIndexes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OIO.Domain.Context.ModerationContext.Aggregates;

namespace OIO.Infrastructure.Persistence.Configurations.ModerationContext;

internal sealed class ReportConfiguration : IEntityTypeConfiguration<Report>
{
    public void Configure(EntityTypeBuilder<Report> builder)
    {
        builder.ToTable("reports");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.ReporterId)
            .HasColumnName("reporter_id")
            .IsRequired();

        builder.Property(r => r.EntityType)
            .HasColumnName("entity_type")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(r => r.EntityId)
            .HasColumnName("entity_id")
            .IsRequired();

        builder.Property(r => r.ReasonCode)
            .HasColumnName("reason_code")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(r => r.Description)
            .HasColumnName("description");

        builder.ComplexProperty(r => r.Status, statusBuilder =>
        {
            statusBuilder.Property(s => s.Id)
                .HasColumnName("status")
                .HasMaxLength(20)
                .IsRequired();
        }).HasComplexCompositeIndex(
            r => new { r.Status.Id, r.AssignedTo },
            indexName: "idx_reports_status_assigned");

        builder.Property(r => r.AssignedTo)
            .HasColumnName("assigned_to");

        builder.Property(r => r.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        // Indexes
        builder.HasIndex(r => new { r.EntityType, r.EntityId })
            .HasDatabaseName("idx_reports_entity");

        builder.HasIndex(r => r.ReporterId)
            .HasDatabaseName("idx_reports_reporter");
    }
}
