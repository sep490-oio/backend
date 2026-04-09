using OIO.Domain.Context.WarehouseContext.Aggregates.OutboundShipments;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseItems;
using OIO.Domain.Context.WarehouseContext.Enums;

namespace OIO.Application.Context.WarehouseContext.DTOs;

public sealed record SellerWarehouseItemListItemDto(
    Guid      WarehouseItemId,
    Guid      ItemId,
    string?   ItemTitle,
    string?   ItemImageUrl,
    string?   InboundPackageCode,
    Guid?     InboundShipmentId,
    string?   StorageLocationLabel,
    DateTime? ReceivedAt,
    DateTime  UpdatedAt,
    string    WarehouseFlowStatus,
    string    WarehouseItemStatusRaw);

public sealed record SellerWarehouseItemReceiptMediaDto(
    Guid     Id,
    string   Url,
    DateTime CreatedAt);

public sealed record SellerWarehouseInspectionDetailDto(
    string DecisionStatus,
    string? DeclaredCondition,
    string? ConditionOnArrival,
    string? InspectionNotes,
    string? DecisionReason,
    DateTime? InspectedAt,
    DateTime? ReviewedAt,
    DateTime? SellerConfirmedAt,
    string? InspectorDisplayName,
    string? ReviewerDisplayName,
    IReadOnlyList<WarehouseInspectionEvidenceDto> Evidence);

public sealed record SellerWarehouseItemDetailDto(
    Guid      WarehouseItemId,
    Guid      ItemId,
    string?   ItemTitle,
    string?   ItemImageUrl,
    string?   InboundPackageCode,
    Guid?     InboundShipmentId,
    string?   StorageLocationLabel,
    DateTime? ReceivedAt,
    DateTime  UpdatedAt,
    string    WarehouseFlowStatus,
    string    WarehouseItemStatusRaw,
    // Inspection
    SellerWarehouseInspectionDetailDto? Inspection,
    // Outbound
    Guid?     OutboundShipmentId,
    string?   OutboundStatus,
    string?   OutboundCarrierTrackingNumber,
    string?   OutboundShippingLabelUrl,
    DateTime? OutboundDispatchedAt,
    DateTime? OutboundDeliveredAt,
    // Media
    List<SellerWarehouseItemReceiptMediaDto> ReceiptMedia,
    // Seller action affordances
    bool CanConfirmInspectedCondition = false);

/// <summary>
/// Derives the seller-facing "warehouse flow" status from the enriched
/// warehouse item state (item status + latest inspection + active outbound).
/// Encapsulated so list and detail queries agree on the value.
/// </summary>
public static class SellerWarehouseFlowStatusResolver
{
    public const string Received                      = "received";
    public const string Stored                        = "stored";
    public const string AwaitingInspection            = "awaiting_inspection";
    public const string AwaitingReview                = "awaiting_review";
    public const string Approved                      = "approved";
    public const string Rejected                      = "rejected";
    public const string ConditionConfirmationRequired = "condition_confirmation_required";
    public const string OutboundBooked                = "outbound_booked";
    public const string Dispatched                    = "dispatched";

    public static string Resolve(
        WarehouseItem warehouseItem,
        WarehouseInspection? inspection,
        OutboundShipment? outbound)
    {
        if (outbound is not null)
        {
            var s = outbound.Status;
            if (s == OutboundShipmentStatus.InTransit ||
                s == OutboundShipmentStatus.PickedUp ||
                s == OutboundShipmentStatus.Delivering ||
                s == OutboundShipmentStatus.Delivered ||
                s == OutboundShipmentStatus.Returning ||
                s == OutboundShipmentStatus.Returned ||
                s == OutboundShipmentStatus.Failed)
            {
                return Dispatched;
            }

            // Pending / Booked / Cancelled-but-exists → booked state
            if (s != OutboundShipmentStatus.Cancelled)
                return OutboundBooked;
        }

        if (inspection is not null)
        {
            var d = inspection.DecisionStatus;
            if (d == WarehouseInspectionDecisionStatus.Approved ||
                d == WarehouseInspectionDecisionStatus.ConditionConfirmed)
                return Approved;
            if (d == WarehouseInspectionDecisionStatus.Rejected)
                return Rejected;
            if (d == WarehouseInspectionDecisionStatus.ConditionConfirmationRequired)
                return ConditionConfirmationRequired;
            if (d == WarehouseInspectionDecisionStatus.PendingReview)
                return AwaitingReview;
        }

        var ws = warehouseItem.Status;
        if (ws == WarehouseItemStatus.Stored && inspection is null)
            return AwaitingInspection;
        if (ws == WarehouseItemStatus.Stored)
            return Stored;
        if (ws == WarehouseItemStatus.Received)
            return Received;

        return ws.Id;
    }
}
