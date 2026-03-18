using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OIO.Domain.Context.CatalogContext.Enums;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.Context.WarehouseContext.Aggregates.InboundShipments;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseItems;
using OIO.Domain.Context.WarehouseContext.Enums;
using OIO.Domain.Context.WarehouseContext.ValueObjects;
using OIO.Domain.Context.WarehouseContext.ValueObjects.Ids;

namespace OIO.Infrastructure.Persistence.Configurations.WarehouseContext;

internal sealed class WarehouseInspectionConfiguration : IEntityTypeConfiguration<WarehouseInspection>
{
    public void Configure(EntityTypeBuilder<WarehouseInspection> builder)
    {
        builder.ToTable("warehouse_inspections");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .ValueGeneratedNever()
            .HasColumnName("id")
            .HasConversion(x => x.Value, v => WarehouseInspectionId.From(v));

        builder.Property(e => e.WarehouseItemId)
            .HasColumnName("warehouse_item_id")
            .IsRequired()
            .HasConversion(x => x.Value, v => WarehouseItemId.From(v));

        builder.Property(e => e.InboundShipmentId)
            .HasColumnName("inbound_shipment_id")
            .IsRequired()
            .HasConversion(x => x.Value, v => InboundShipmentId.From(v));

        builder.Property(e => e.ItemId)
            .HasColumnName("item_id")
            .IsRequired();

        builder.Property(e => e.DeclaredCondition)
            .HasColumnName("declared_condition")
            .HasMaxLength(20)
            .IsRequired()
            .HasConversion(x => x.Id, v => ItemCondition.FromId(v).Value);

        builder.Property(e => e.ConditionOnArrival)
            .HasColumnName("condition_on_arrival")
            .HasMaxLength(20)
            .IsRequired()
            .HasConversion(x => x.Id, v => WarehouseItemCondition.FromId(v).GetValueOrThrow());

        builder.Property(e => e.InspectionNotes)
            .HasColumnName("inspection_notes")
            .HasMaxLength(1000);

        builder.Property(e => e.Evidence)
            .HasColumnName("evidence_json")
            .HasColumnType("jsonb")
            .IsRequired()
            .HasConversion(x => x.RawJson, v => InspectionEvidence.From(v));

        builder.Property(e => e.DecisionStatus)
            .HasColumnName("decision_status")
            .HasMaxLength(50)
            .IsRequired()
            .HasConversion(x => x.Id, v => WarehouseInspectionDecisionStatus.FromId(v).GetValueOrThrow());

        builder.Property(e => e.DecisionReason)
            .HasColumnName("decision_reason")
            .HasMaxLength(1000);

        builder.Property(e => e.InspectedBy)
            .HasColumnName("inspected_by")
            .IsRequired()
            .HasConversion(x => x.Value, v => UserId.From(v));

        builder.Property(e => e.InspectedAt)
            .HasColumnName("inspected_at")
            .IsRequired();

        builder.Property(e => e.ReviewedBy)
            .HasColumnName("reviewed_by")
            .HasConversion(
                x => x.HasValue ? x.Value.Value : (Guid?)null,
                v => v.HasValue ? UserId.From(v.Value) : null);

        builder.Property(e => e.ReviewedAt)
            .HasColumnName("reviewed_at");

        builder.Property(e => e.SellerConfirmedAt)
            .HasColumnName("seller_confirmed_at");

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.Property(e => e.ModifiedAt)
            .HasColumnName("modified_at");

        builder.HasOne<WarehouseItem>()
            .WithOne()
            .HasForeignKey<WarehouseInspection>(e => e.WarehouseItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<InboundShipment>()
            .WithMany()
            .HasForeignKey(e => e.InboundShipmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => e.InboundShipmentId)
            .IsUnique()
            .HasDatabaseName("idx_unique_warehouse_inspections_inbound_shipment_id");

        builder.HasIndex(e => e.WarehouseItemId)
            .IsUnique()
            .HasDatabaseName("idx_unique_warehouse_inspections_warehouse_item_id");

        builder.HasIndex(e => e.ItemId)
            .HasDatabaseName("idx_warehouse_inspections_item_id");

        builder.HasIndex(e => e.DecisionStatus)
            .HasDatabaseName("idx_warehouse_inspections_decision_status");

        builder.Ignore(e => e.DomainEvents);
    }
}
