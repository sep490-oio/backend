using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.WarehouseContext.DTOs;
using OIO.Application.Context.WarehouseContext.Queries.GetInboundPackages;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.CatalogContext.Aggregates.Items;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.OrderContext.Enums;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.WarehouseContext.Aggregates.InboundShipments;
using OIO.Domain.Context.WarehouseContext.Aggregates.OutboundShipments;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseItems;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseStorage;
using OIO.Domain.Context.WarehouseContext.Enums;
using OIO.Domain.Context.WarehouseContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;
using ItemId = OIO.Domain.Context.CatalogContext.ValueObjects.Ids.ItemId;

namespace OIO.Application.Context.WarehouseContext.Queries.GetWarehouseItemById;

public sealed record GetWarehouseItemByIdQuery(Guid WarehouseItemId)
    : IQuery<WarehouseItemDetailDto>;

internal sealed class GetWarehouseItemByIdQueryHandler(IDbContext db)
    : IQueryHandler<GetWarehouseItemByIdQuery, WarehouseItemDetailDto>
{
    public async Task<Result<WarehouseItemDetailDto, Error>> Handle(
        GetWarehouseItemByIdQuery request,
        CancellationToken cancellationToken)
    {
        var whId = WarehouseItemId.From(request.WarehouseItemId);

        var w = await db.Set<WarehouseItem>().AsNoTracking()
            .Include(x => x.Media)
            .FirstOrDefaultAsync(x => x.Id == whId, cancellationToken);

        if (w is null)
            return Result.Failure<WarehouseItemDetailDto, Error>(
                Error.NotFound("WarehouseItem.NotFound", $"Warehouse item {request.WarehouseItemId} not found"));

        var shipment = await db.Set<InboundShipment>().AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == w.InboundShipmentId, cancellationToken);
        var (receiptMedia, _) = shipment != null 
            ? PackageStateResolver.ExtractReceipt(shipment) 
            : (Array.Empty<string>(), null);

        var itemId = ItemId.From(w.ItemId);
        var item = await db.Set<Item>().AsNoTracking()
            .Include(i => i.Media)
            .FirstOrDefaultAsync(i => i.Id == itemId, cancellationToken);

        User? seller = null;
        if (shipment != null)
        {
            seller = await db.Set<User>().AsNoTracking()
                .Include(u => u.Profile)
                .FirstOrDefaultAsync(u => u.Id == shipment.SellerId, cancellationToken);
        }

        WarehouseStorageLocation? location = null;
        if (w.StorageLocationId is { } locId)
        {
            location = await db.Set<WarehouseStorageLocation>().AsNoTracking()
                .FirstOrDefaultAsync(l => l.Id == locId, cancellationToken);
        }

        var primaryMedia = item?.Media.FirstOrDefault(m => m.IsPrimary)
                           ?? item?.Media.FirstOrDefault();

        // ── Staff action affordances (batch-enrich, no nested VO subqueries) ──
        // 1) Can assign/move location: WarehouseItem.Store only accepts Received/Inspected
        //    and rejects if a location is already assigned.
        var canAssignOrMoveLocation =
            (w.Status == WarehouseItemStatus.Received || w.Status == WarehouseItemStatus.Inspected)
            && w.StorageLocationId is null;

        // 2) Active outbound shipment for this warehouse item (non-terminal, not cancelled).
        var outboundShipments = await db.Set<OutboundShipment>().AsNoTracking()
            .Where(o => o.WarehouseItemId == w.Id)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(cancellationToken);

        var activeOutbound = outboundShipments.FirstOrDefault(o =>
            o.Status != OutboundShipmentStatus.Cancelled);

        var canViewOutboundShipment = activeOutbound is not null;
        Guid? outboundShipmentId = activeOutbound?.Id.Value;

        // 3) Can book outbound: mirror GetWarehouseStaffOutboundQueue rules —
        //    warehouse item must be in a shippable status (Received/Inspected/Stored),
        //    order must be Processing, and no active outbound shipment may already exist.
        bool canBookOutbound = false;
        Guid? outboundBookingOrderId = null;
        var shippable = w.Status == WarehouseItemStatus.Stored
                        || w.Status == WarehouseItemStatus.Received
                        || w.Status == WarehouseItemStatus.Inspected;
        if (shippable && activeOutbound is null)
        {
            // Find the Processing order whose auction references this item.
            var auction = await db.Set<Auction>().AsNoTracking()
                .Where(a => a.ItemId == itemId)
                .OrderByDescending(a => a.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (auction is not null)
            {
                var order = await db.Set<Order>().AsNoTracking()
                    .Where(o => o.AuctionId == auction.Id && o.Status == OrderStatus.Processing)
                    .OrderByDescending(o => o.CreatedAt)
                    .FirstOrDefaultAsync(cancellationToken);

                if (order is not null)
                {
                    // Ensure no other active outbound shipment exists for this order.
                    var orderHasActiveOutbound = await db.Set<OutboundShipment>().AsNoTracking()
                        .AnyAsync(s => s.OrderId == order.Id &&
                                       s.Status != OutboundShipmentStatus.Cancelled &&
                                       s.Status != OutboundShipmentStatus.Failed &&
                                       s.Status != OutboundShipmentStatus.Returned &&
                                       s.Status != OutboundShipmentStatus.Delivered,
                            cancellationToken);

                    if (!orderHasActiveOutbound)
                    {
                        canBookOutbound = true;
                        outboundBookingOrderId = order.Id.Value;
                    }
                }
            }
        }

        var dto = new WarehouseItemDetailDto(
            Id:                   w.Id.Value,
            Status:               w.Status.Id,
            ReceivedAt:           w.ReceivedAt,
            CreatedAt:            w.CreatedAt,
            ModifiedAt:           w.ModifiedAt,
            StorageLocationId:    w.StorageLocationId?.Value,
            StorageLocationLabel: location?.Label,
            InboundShipmentId:    w.InboundShipmentId.Value,
            InboundShipmentCode:  shipment?.ClientOrderCode,
            ItemId:               w.ItemId,
            ItemTitle:            item?.Title.Value,
            ItemImageUrl:         primaryMedia?.Info.SecureUrl,
            Condition:            item?.Condition.Id,
            Description:          item?.Description,
            SellerId:             shipment?.SellerId.Value,
            SellerName:           seller?.Profile?.Name.DisplayName ?? seller?.UserName.Value,
            Media:                w.Media
                .OrderBy(m => m.SortOrder)
                .Select(m => new WarehouseItemMediaDto(
                    Id:           m.Id.Value,
                    ResourceType: m.ResourceType,
                    IsPrimary:    m.IsPrimary,
                    SortOrder:    m.SortOrder,
                    SecureUrl:    m.Info.SecureUrl!,
                    FileName:     m.Info.FileName))
                .ToList(),
            ReceiptPhotos:        receiptMedia,
            CanAssignOrMoveLocation: canAssignOrMoveLocation,
            CanBookOutbound:         canBookOutbound,
            OutboundBookingOrderId:  outboundBookingOrderId,
            CanViewOutboundShipment: canViewOutboundShipment,
            OutboundShipmentId:      outboundShipmentId);

        return Result.Success<WarehouseItemDetailDto, Error>(dto);
    }
}
