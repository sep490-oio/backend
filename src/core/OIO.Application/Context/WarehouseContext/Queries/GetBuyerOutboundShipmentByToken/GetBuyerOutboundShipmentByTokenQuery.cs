using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.Services;
using OIO.Application.Context.WarehouseContext.DTOs;
using OIO.Application.Context.WarehouseContext.Services;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.ModerationContext.Aggregates.Disputes;
using OIO.Domain.Context.ModerationContext.Enums;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.OrderContext.Enums;
using OIO.Domain.Context.WarehouseContext.Aggregates.OutboundShipments;
using OIO.Domain.Context.Shared.Entities;
using OIO.Domain.Context.Shared.ValueObjects.Ids;
using OIO.Domain.Context.WarehouseContext.Enums;
using OIO.Domain.Context.WarehouseContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;
using Error = OIO.Domain.SeedWork.Errors.Error;

namespace OIO.Application.Context.WarehouseContext.Queries.GetBuyerOutboundShipmentByToken;

public sealed record GetBuyerOutboundShipmentByTokenQuery(string Token)
    : IQuery<BuyerOutboundShipmentDetailDto>;

internal sealed class GetBuyerOutboundShipmentByTokenQueryHandler(
    IDbContext db,
    IOutboundShipmentQrTokenService tokenService,
    ICurrentUser currentUser)
    : IQueryHandler<GetBuyerOutboundShipmentByTokenQuery, BuyerOutboundShipmentDetailDto>
{
    public async Task<Result<BuyerOutboundShipmentDetailDto, Error>> Handle(
        GetBuyerOutboundShipmentByTokenQuery request,
        CancellationToken cancellationToken)
    {
        var validation = tokenService.Validate(request.Token);
        if (validation.IsFailure) return validation.Error;
        var payload = validation.Value;

        if (currentUser.UserId.Value != payload.BuyerId)
            return Error.Forbidden(
                "OutboundShipmentQr.Forbidden",
                "This shipment token does not belong to the current user.");

        var shipmentId = OutboundShipmentId.From(payload.ShipmentId);
        var shipment = await db.Set<OutboundShipment>()
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == shipmentId, cancellationToken);

        if (shipment is null)
            return Error.NotFound(
                "OutboundShipmentQr.NotFound",
                "Shipment was not found.");

        if (shipment.QrTokenRevokedAt is not null)
            return Error.Validation(
                "token",
                "OutboundShipmentQr.Revoked",
                "This shipment QR token has been revoked.");

        if (payload.Version != shipment.QrTokenVersion)
            return Error.Validation(
                "token",
                "OutboundShipmentQr.StaleToken",
                "This shipment QR token is stale and no longer valid.");

        if (!BuyerOutboundShipmentDetailBuilder.IsAllowedShipmentStatus(shipment.Status))
            return Error.Validation(
                "shipment",
                "OutboundShipmentQr.ShipmentClosed",
                "This shipment is already closed.");

        var order = await db.Set<Order>()
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == shipment.OrderId, cancellationToken);

        if (order is null || order.Id.Value != payload.OrderId || order.BuyerId.Value != payload.BuyerId)
            return Error.Forbidden(
                "OutboundShipmentQr.Forbidden",
                "This shipment token does not belong to the current user.");

        return await BuyerOutboundShipmentDetailBuilder.BuildAsync(db, shipment, order, cancellationToken);
    }
}

/// <summary>
/// Shared projection logic so the detail query and the acknowledge-received
/// command return the same DTO shape. Also exposes the allowed-status gate
/// used by the query for early rejection.
/// </summary>
public static class BuyerOutboundShipmentDetailBuilder
{
    public static bool IsAllowedShipmentStatus(OutboundShipmentStatus status) =>
        status == OutboundShipmentStatus.PickedUp ||
        status == OutboundShipmentStatus.InTransit ||
        status == OutboundShipmentStatus.Delivering ||
        status == OutboundShipmentStatus.Delivered;

    public static async Task<BuyerOutboundShipmentDetailDto> BuildAsync(
        IDbContext db,
        OutboundShipment shipment,
        Order order,
        CancellationToken cancellationToken)
    {
        var auction = await db.Set<Auction>()
            .AsNoTracking()
            .Include(a => a.Item)
                .ThenInclude(i => i.Media)
            .FirstOrDefaultAsync(a => a.Id == order.AuctionId, cancellationToken);

        var item = auction?.Item;
        var primaryImageUrl = item?.Media
            .Where(m => m.IsPrimary)
            .OrderBy(m => m.SortOrder)
            .Select(m => m.Info.SecureUrl)
            .FirstOrDefault();

        string? composedAddress = null;
        if (order.Shipping is not null)
        {
            composedAddress = order.Shipping.IsStructured
                ? string.Join(", ", new[]
                    {
                        order.Shipping.Street,
                        order.Shipping.Ward,
                        order.Shipping.District,
                        order.Shipping.City,
                        order.Shipping.PostalCode
                    }.Where(p => !string.IsNullOrWhiteSpace(p)))
                : order.Shipping.Address;
        }

        // Active dispute = any dispute row for this order that isn't in a
        // terminal state. Mirrors what the order detail page shows as "an
        // open case exists against this order".
        var hasActiveDispute = await db.Set<Dispute>()
            .AsNoTracking()
            .AnyAsync(
                d => d.OrderId == order.Id &&
                     d.Status != DisputeStatus.Resolved &&
                     d.Status != DisputeStatus.Closed &&
                     d.Status != DisputeStatus.Cancelled,
                cancellationToken);

        var qrAvailable = shipment.QrPayload is not null && shipment.QrTokenRevokedAt is null;

        var shipmentAllowed = IsAllowedShipmentStatus(shipment.Status);
        var canAcknowledgeReceived = shipmentAllowed && shipment.BuyerReceivedPackageAt is null;

        // Evidence photos — load if the collection is populated (tracked entity)
        // or query separately (no-tracking projection).
        var evidence = shipment.Evidence.Count > 0
            ? shipment.Evidence.ToList()
            : await db.Set<OutboundShipmentEvidence>()
                .AsNoTracking()
                .Where(e => e.ShipmentId == shipment.Id)
                .ToListAsync(cancellationToken);

        // Batch-load MediaUpload rows for any evidence that lacks a SecureUrl —
        // legacy rows had the URL omitted at creation time and must be resolved
        // from the linked MediaUpload.Info.SecureUrl.
        var missingUrlIds = evidence
            .Where(e => string.IsNullOrEmpty(e.SecureUrl))
            .Select(e => e.MediaUploadId)
            .Distinct()
            .ToList();

        Dictionary<MediaUploadId, string> mediaUploadUrls = [];
        if (missingUrlIds.Count > 0)
        {
            mediaUploadUrls = await db.Set<MediaUpload>()
                .AsNoTracking()
                .Where(m => missingUrlIds.Contains(m.Id))
                .ToDictionaryAsync(m => m.Id, m => m.Info.SecureUrl ?? string.Empty, cancellationToken);
        }

        var hasBuyerReceiptProof = evidence.Any(e => e.Category == "buyer_receipt_photo");

        // Mirror IOrderReceiptService.ConfirmAsync preconditions: the order
        // must be in Delivered so escrow release is legal. Blocked when a
        // dispute is active. Receipt proof is now a precondition.
        var canAccept = order.Status == OrderStatus.Delivered && hasBuyerReceiptProof && !hasActiveDispute;

        var canSubmitReceiptProof = order.Status == OrderStatus.Delivered && !hasBuyerReceiptProof && !hasActiveDispute;

        // Order-level dispute eligibility: buyer may open a dispute while the
        // order is post-payment and pre-terminal, and no active dispute
        // already exists. Mirrors Order.MarkAsDisputed guards.
        var canOpenDispute = !hasActiveDispute &&
            (order.Status == OrderStatus.Delivered ||
             order.Status == OrderStatus.Shipped ||
             order.Status == OrderStatus.PickedUp ||
             order.Status == OrderStatus.OnDelivering);

        // Proof-of-delivery is advisory; available whenever the shipment is
        // delivered regardless of whether proof was already submitted (buyers
        // can add more photos). Warehouse outbound proof is a v2 feature so
        // the flag gates a CTA stub on the FE.
        var canSubmitProof = order.Status == OrderStatus.Delivered;

        // Auto-set CanAcknowledgeReceived: if proof exists, ack is already done
        var effectiveCanAck = shipmentAllowed && shipment.BuyerReceivedPackageAt is null && !hasBuyerReceiptProof;

        IReadOnlyList<EvidencePhotoDto> ProjectPhotos(IEnumerable<OutboundShipmentEvidence> items, string category) =>
            items.Where(e => e.Category == category)
                 .OrderBy(e => e.CreatedAt)
                 .Select(e =>
                 {
                     var url = !string.IsNullOrEmpty(e.SecureUrl)
                         ? e.SecureUrl
                         : mediaUploadUrls.GetValueOrDefault(e.MediaUploadId, string.Empty);
                     return (url, e);
                 })
                 .Where(t => !string.IsNullOrEmpty(t.url))
                 .Select(t => new EvidencePhotoDto(t.e.Id.Value, t.url, t.e.CreatedAt))
                 .ToList();

        return new BuyerOutboundShipmentDetailDto(
            ShipmentId: shipment.Id.Value,
            OrderId: order.Id.Value,
            OrderNumber: order.OrderNumber.Value,
            Status: shipment.Status.Id,
            ClientOrderCode: shipment.ClientOrderCode,
            CarrierTrackingNumber: shipment.CarrierTrackingNumber,
            ExternalCarrierName: shipment.ExternalCarrierName,
            ItemTitle: item?.Title.Value,
            ItemPrimaryImageUrl: primaryImageUrl,
            RecipientName: order.Shipping?.RecipientName,
            ComposedAddress: composedAddress,
            DispatchedAt: shipment.DispatchedAt,
            DeliveredAt: shipment.DeliveredAt,
            QrAvailable: qrAvailable,
            BuyerReceivedPackageAt: shipment.BuyerReceivedPackageAt,
            BuyerAcceptedAt: shipment.BuyerAcceptedAt,
            CanAcknowledgeReceived: effectiveCanAck,
            CanAccept: canAccept,
            CanOpenDispute: canOpenDispute,
            HasActiveDispute: hasActiveDispute,
            DecisionWindowEndsAt: order.DecisionWindowEndsAt,
            CanSubmitProof: canSubmitProof,
            HasBuyerReceiptProof: hasBuyerReceiptProof,
            CanSubmitReceiptProof: canSubmitReceiptProof,
            PackagePhotos: ProjectPhotos(evidence, "staff_package_photo"),
            HandoverPhotos: ProjectPhotos(evidence, "staff_handover_photo"),
            BuyerReceiptPhotos: ProjectPhotos(evidence, "buyer_receipt_photo"));
    }
}
