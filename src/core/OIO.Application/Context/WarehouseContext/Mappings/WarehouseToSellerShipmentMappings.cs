using OIO.Application.Context.WarehouseContext.DTOs;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseItems;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseToSellerShipments;

namespace OIO.Application.Context.WarehouseContext.Mappings;

internal static class WarehouseToSellerShipmentMappings
{
    public static WarehouseToSellerShipmentDto ToDto(
        this WarehouseToSellerShipment shipment,
        WarehouseToSellerShipmentItemSummaryDto? itemSummary = null,
        string? sellerDisplayName = null) =>
        new(
            Id:                    shipment.Id.Value,
            WarehouseItemId:       shipment.WarehouseItemId.Value,
            WarehouseInspectionId: shipment.WarehouseInspectionId.Value,
            SellerId:              shipment.SellerId.Value,
            SellerAddressSnapshot: shipment.SellerAddressSnapshot,
            RejectionReason:       shipment.RejectionReason,
            ProviderCode:          shipment.ProviderCode,
            TrackingNumber:        shipment.TrackingNumber,
            ShippedAt:             shipment.ShippedAt,
            DeliveredAt:           shipment.DeliveredAt,
            SellerConfirmedAt:     shipment.SellerConfirmedAt,
            DeliveryFailureReason: shipment.DeliveryFailureReason,
            Status:                shipment.Status.Id,
            CreatedAt:             shipment.CreatedAt,
            ModifiedAt:            shipment.ModifiedAt,
            Item:                  itemSummary,
            SellerDisplayName:     sellerDisplayName,
            QrToken:               shipment.QrToken,
            HasReceiptEvidence:    shipment.HasReceiptEvidence,
            Evidence:              shipment.Evidence.Select(e => new WarehouseToSellerShipmentEvidenceDto(
                Id:            e.Id.Value,
                ShipmentId:    e.ShipmentId.Value,
                Category:      e.Category,
                MediaUploadId: e.MediaUploadId.Value,
                SecureUrl:     e.SecureUrl,
                FileName:      e.FileName,
                ResourceType:  e.ResourceType,
                CreatedAt:     e.CreatedAt,
                CreatedBy:     e.CreatedBy.Value)).ToList());

    public static WarehouseToSellerShipmentItemSummaryDto ToSummary(
        this WarehouseItem warehouseItem,
        string? itemTitle,
        string? primaryImageUrl) =>
        new(
            WarehouseItemId:       warehouseItem.Id.Value,
            ItemId:                warehouseItem.ItemId,
            ItemTitle:             itemTitle,
            PrimaryImageUrl:       primaryImageUrl,
            WarehouseItemStatus:   warehouseItem.Status.Id);
}
