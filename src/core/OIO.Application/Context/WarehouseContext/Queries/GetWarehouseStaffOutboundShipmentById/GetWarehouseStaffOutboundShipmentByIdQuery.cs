using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.WarehouseContext.DTOs;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.WarehouseContext.Aggregates.OutboundShipments;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseItems;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseStorage;
using OIO.Domain.Context.WarehouseContext.Enums;
using OIO.Domain.Context.WarehouseContext.Errors;
using OIO.Domain.Context.WarehouseContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.WarehouseContext.Queries.GetWarehouseStaffOutboundShipmentById;

public sealed record GetWarehouseStaffOutboundShipmentByIdQuery(Guid ShipmentId)
    : IQuery<WarehouseStaffOutboundShipmentDetailDto>;

internal sealed class GetWarehouseStaffOutboundShipmentByIdQueryHandler(IDbContext db)
    : IQueryHandler<GetWarehouseStaffOutboundShipmentByIdQuery, WarehouseStaffOutboundShipmentDetailDto>
{
    public async Task<Result<WarehouseStaffOutboundShipmentDetailDto, Error>> Handle(
        GetWarehouseStaffOutboundShipmentByIdQuery request,
        CancellationToken cancellationToken)
    {
        var shipmentId = OutboundShipmentId.From(request.ShipmentId);

        var shipment = await db.Set<OutboundShipment>()
            .AsNoTracking()
            .Include(s => s.TrackingEvents)
            .FirstOrDefaultAsync(s => s.Id == shipmentId, cancellationToken);

        if (shipment is null)
            return WarehouseErrors.OutboundShipment.NotFound(request.ShipmentId.ToString());

        var dto = await StaffOutboundShipmentDetailBuilder.BuildAsync(db, shipment, cancellationToken);
        return dto;
    }
}

/// <summary>
/// Shared detail-DTO builder reused by the staff shipment-by-id query and the
/// manual-status update command (which returns the refreshed detail payload).
/// </summary>
internal static class StaffOutboundShipmentDetailBuilder
{
    public static async Task<WarehouseStaffOutboundShipmentDetailDto> BuildAsync(
        IDbContext db,
        OutboundShipment shipment,
        CancellationToken cancellationToken)
    {
        var order = await db.Set<Order>()
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == shipment.OrderId, cancellationToken);

        Auction? auction = null;
        if (order is not null)
        {
            auction = await db.Set<Auction>()
                .AsNoTracking()
                .Include(a => a.Item)
                    .ThenInclude(i => i.Media)
                .FirstOrDefaultAsync(a => a.Id == order.AuctionId, cancellationToken);
        }

        var item = auction?.Item;
        var primaryImageUrl = item?.Media
            .Where(m => m.IsPrimary)
            .OrderBy(m => m.SortOrder)
            .Select(m => m.Info.SecureUrl)
            .FirstOrDefault();

        WarehouseItem? warehouseItem = null;
        if (shipment.WarehouseItemId is { } whId)
        {
            warehouseItem = await db.Set<WarehouseItem>()
                .AsNoTracking()
                .FirstOrDefaultAsync(wi => wi.Id == whId, cancellationToken);
        }

        string? storageLocationLabel = null;
        if (warehouseItem?.StorageLocationId is { } locId)
        {
            var loc = await db.Set<WarehouseStorageLocation>()
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.Id == locId, cancellationToken);
            storageLocationLabel = loc?.Label;
        }

        string? composedAddress = null;
        if (order?.Shipping is not null)
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

        var events = BuildTimeline(shipment);
        var allowedManual = ComputeAllowedManualStatuses(shipment);

        return new WarehouseStaffOutboundShipmentDetailDto(
            ShipmentId: shipment.Id.Value,
            OrderId: shipment.OrderId.Value,
            OrderNumber: order?.OrderNumber.Value ?? string.Empty,
            Status: shipment.Status.Id,
            ShipmentMode: shipment.ShipmentMode.Id,
            ProviderCode: shipment.ProviderCode.Id,
            ExternalCarrierName: shipment.ExternalCarrierName,
            CarrierTrackingNumber: shipment.CarrierTrackingNumber,
            ShippingLabelUrl: shipment.ShippingLabelUrl,
            CreatedAt: shipment.CreatedAt,
            PackedAt: shipment.PackedAt,
            DispatchedAt: shipment.DispatchedAt,
            DeliveredAt: shipment.DeliveredAt,
            WarehouseItemId: shipment.WarehouseItemId?.Value,
            ItemId: item?.Id.Value ?? Guid.Empty,
            ItemTitle: item?.Title.Value,
            ItemPrimaryImageUrl: primaryImageUrl,
            StorageLocationLabel: storageLocationLabel,
            RecipientName: order?.Shipping?.RecipientName,
            RecipientPhone: order?.Shipping?.Phone,
            ComposedAddress: composedAddress,
            Events: events,
            AllowedManualStatuses: allowedManual,
            QrPayload: shipment.QrPayload,
            QrCodeUrl: shipment.QrCodeUrl,
            QrTokenVersion: shipment.QrTokenVersion,
            QrTokenIssuedAt: shipment.QrTokenIssuedAt,
            QrTokenRevokedAt: shipment.QrTokenRevokedAt);
    }

    private static IReadOnlyList<OutboundShipmentTimelineEventDto> BuildTimeline(OutboundShipment shipment)
    {
        var events = new List<OutboundShipmentTimelineEventDto>
        {
            new("created", "Shipment created", shipment.CreatedAt, "system", null)
        };

        if (shipment.PackedAt is { } packedAt)
            events.Add(new("packed", "Packed", packedAt, "system", null));

        if (shipment.DispatchedAt is { } dispatchedAt)
            events.Add(new("dispatched", "Dispatched", dispatchedAt, "system", null));

        if (shipment.DeliveredAt is { } deliveredAt)
            events.Add(new("delivered", "Delivered", deliveredAt, "system", null));

        foreach (var t in shipment.TrackingEvents)
        {
            events.Add(new OutboundShipmentTimelineEventDto(
                Code: t.NormalizedStatus.Id,
                Label: t.CarrierStatusDesc ?? t.NormalizedStatus.Id,
                OccurredAt: t.EventTime,
                Source: "carrier",
                Note: t.ReasonDescription ?? t.Location));
        }

        return events.OrderBy(e => e.OccurredAt).ToList();
    }

    public static IReadOnlyList<string> ComputeAllowedManualStatuses(OutboundShipment shipment)
    {
        if (shipment.ShipmentMode != OutboundShipmentMode.ExternalCarrier)
            return Array.Empty<string>();

        if (shipment.Status == OutboundShipmentStatus.Booked)
            return new[] { "picked_up", "failed", "returning" };

        if (shipment.Status == OutboundShipmentStatus.PickedUp)
            return new[] { "delivering", "failed", "returning" };

        if (shipment.Status == OutboundShipmentStatus.Delivering)
            return new[] { "delivered", "failed", "returning" };

        if (shipment.Status == OutboundShipmentStatus.Returning)
            return new[] { "returned" };

        return Array.Empty<string>();
    }
}
