using OIO.Application.Context.WarehouseContext.DTOs;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseItems;

namespace OIO.Application.Context.WarehouseContext.Mappings;

internal static class WarehouseInspectionMappings
{
    public static WarehouseInspectionDto ToDto(this WarehouseInspection inspection) =>
        new(
            Id: inspection.Id.Value,
            WarehouseItemId: inspection.WarehouseItemId.Value,
            InboundShipmentId: inspection.InboundShipmentId.Value,
            ItemId: inspection.ItemId,
            DeclaredCondition: inspection.DeclaredCondition.Id,
            ConditionOnArrival: inspection.ConditionOnArrival.Id,
            InspectionNotes: inspection.InspectionNotes,
            DecisionStatus: inspection.DecisionStatus.Id,
            DecisionReason: inspection.DecisionReason,
            InspectedBy: inspection.InspectedBy.Value,
            InspectedAt: inspection.InspectedAt,
            ReviewedBy: inspection.ReviewedBy?.Value,
            ReviewedAt: inspection.ReviewedAt,
            SellerConfirmedAt: inspection.SellerConfirmedAt,
            CreatedAt: inspection.CreatedAt,
            ModifiedAt: inspection.ModifiedAt,
            Evidence: inspection.Evidence.ToSnapshots()
                .Select(snapshot => new WarehouseInspectionEvidenceDto(
                    snapshot.PublicId,
                    snapshot.Folder,
                    snapshot.SecureUrl,
                    snapshot.FileName,
                    snapshot.Bytes,
                    snapshot.Format,
                    snapshot.Width,
                    snapshot.Height,
                    snapshot.DurationSeconds))
                .ToList());
}
