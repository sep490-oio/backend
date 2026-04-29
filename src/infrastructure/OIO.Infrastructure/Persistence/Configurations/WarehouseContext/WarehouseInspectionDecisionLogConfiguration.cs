using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseItems;
using OIO.Domain.Context.WarehouseContext.ValueObjects.Ids;

namespace OIO.Infrastructure.Persistence.Configurations.WarehouseContext;

internal sealed class WarehouseInspectionDecisionLogConfiguration : IEntityTypeConfiguration<WarehouseInspectionDecisionLog>
{
    public void Configure(EntityTypeBuilder<WarehouseInspectionDecisionLog> builder)
    {
        builder.ToTable("warehouse_inspection_decision_logs");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .ValueGeneratedNever()
            .HasColumnName("id")
            .HasConversion(x => x.Value, v => WarehouseInspectionDecisionLogId.From(v));

        builder.Property(e => e.InspectionId)
            .HasColumnName("inspection_id")
            .IsRequired()
            .HasConversion(x => x.Value, v => WarehouseInspectionId.From(v));

        builder.Property(e => e.DecisionType)
            .HasColumnName("decision_type")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.Reason)
            .HasColumnName("reason")
            .HasMaxLength(1000);

        builder.Property(e => e.ActorId)
            .HasColumnName("actor_id")
            .HasConversion(
                x => x.HasValue ? x.Value.Value : (Guid?)null,
                v => v.HasValue ? UserId.From(v.Value) : null);

        builder.Property(e => e.ActorRole)
            .HasColumnName("actor_role")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        // Owned by WarehouseInspection — relationship configured from the
        // parent aggregate via HasMany(DecisionLogs) so EF can navigate via
        // the readonly collection backed by `_decisionLogs`.

        builder.HasIndex(e => e.InspectionId)
            .HasDatabaseName("idx_warehouse_inspection_decision_logs_inspection_id");
    }
}
