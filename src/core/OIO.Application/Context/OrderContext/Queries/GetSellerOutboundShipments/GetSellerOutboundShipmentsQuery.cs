using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.OrderContext.DTOs;
using OIO.Application.Context.UserContext.Services;
using OIO.Application.Extensions;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.OrderContext.ValueObjects.Ids;
using OIO.Domain.Context.WarehouseContext.Aggregates.OutboundShipments;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.OrderContext.Queries.GetSellerOutboundShipments;

/// <summary>
/// Seller-safe list of outbound shipments. Returns shipments whose owning
/// order is sold by the current user, enriched with product + recipient
/// context so the seller UI doesn't have to round-trip auctions/orders.
/// Route: GET /api/me/orders/seller-direct-ship/outbound-shipments
/// </summary>
public sealed record GetSellerOutboundShipmentsQuery(PagedParameters Parameters)
    : IQuery<PagedList<SellerOutboundShipmentDto>>;

internal sealed class GetSellerOutboundShipmentsQueryHandler(
    IDbContext dbContext,
    ICurrentUser currentUser)
    : IQueryHandler<GetSellerOutboundShipmentsQuery, PagedList<SellerOutboundShipmentDto>>
{
    public async Task<Result<PagedList<SellerOutboundShipmentDto>, Error>> Handle(
        GetSellerOutboundShipmentsQuery request,
        CancellationToken cancellationToken)
    {
        var parameters = request.Parameters;

        // Find seller's order ids first so we can filter shipments by ownership.
        var sellerOrderIds = await dbContext.Set<Order>()
            .AsNoTracking()
            .Where(o => o.SellerId == currentUser.UserId)
            .Select(o => o.Id)
            .ToListAsync(cancellationToken);

        if (sellerOrderIds.Count == 0)
        {
            return new List<SellerOutboundShipmentDto>()
                .ToPagedList(0, parameters);
        }

        var shipmentsQuery = dbContext.Set<OutboundShipment>()
            .AsNoTracking()
            .Include(s => s.TrackingEvents)
            .Where(s => sellerOrderIds.Contains(s.OrderId));

        var totalCounts = await shipmentsQuery.CountAsync(cancellationToken);

        var pagedShipments = await shipmentsQuery
            .OrderByDescending(s => s.CreatedAt)
            .Page(parameters)
            .ToListAsync(cancellationToken);

        // Batch-load orders (with shipping snapshot) and related auctions + items.
        var orderIds = pagedShipments.Select(s => s.OrderId).Distinct().ToList();
        var orders = await dbContext.Set<Order>()
            .AsNoTracking()
            .Where(o => orderIds.Contains(o.Id))
            .ToListAsync(cancellationToken);
        var ordersById = orders.ToDictionary(o => o.Id);

        var auctionIds = orders.Select(o => o.AuctionId).Distinct().ToList();
        var auctions = await dbContext.Set<Auction>()
            .AsNoTracking()
            .Include(a => a.Item)
                .ThenInclude(i => i.Media)
            .Where(a => auctionIds.Contains(a.Id))
            .ToListAsync(cancellationToken);
        var auctionsById = auctions.ToDictionary(a => a.Id);

        var dtos = pagedShipments
            .Select(s => SellerOutboundShipmentMapping.BuildDto(s, ordersById, auctionsById))
            .ToList();

        return dtos.ToPagedList(totalCounts, parameters);
    }
}

/// <summary>
/// Shared mapping helper so the list and detail handlers produce identical shape.
/// </summary>
internal static class SellerOutboundShipmentMapping
{
    public static SellerOutboundShipmentDto BuildDto(
        OutboundShipment shipment,
        IReadOnlyDictionary<OrderId, Order> ordersById,
        IReadOnlyDictionary<AuctionId, Auction> auctionsById)
    {
        ordersById.TryGetValue(shipment.OrderId, out var order);

        SellerOutboundShipmentItemDto? itemDto = null;
        SellerOutboundShipmentRecipientDto? recipientDto = null;

        if (order is not null)
        {
            if (auctionsById.TryGetValue(order.AuctionId, out var auction) && auction.Item is not null)
            {
                var primaryImageUrl = auction.Item.Media
                    .Where(m => m.IsPrimary)
                    .OrderBy(m => m.SortOrder)
                    .Select(m => m.Info.SecureUrl)
                    .FirstOrDefault();

                itemDto = new SellerOutboundShipmentItemDto(
                    ItemId: auction.Item.Id.Value,
                    AuctionId: auction.Id.Value,
                    ItemTitle: auction.Item.Title.Value,
                    PrimaryImageUrl: primaryImageUrl,
                    FinalPrice: order.Pricing.ItemPrice.Amount,
                    Currency: order.Currency);
            }

            if (order.Shipping is not null)
            {
                recipientDto = new SellerOutboundShipmentRecipientDto(
                    RecipientName: order.Shipping.RecipientName,
                    PhoneNumber: order.Shipping.Phone,
                    ComposedAddress: order.Shipping.Address);
            }
        }

        var trackingEvents = shipment.TrackingEvents
            .OrderByDescending(e => e.EventTime)
            .Select(e => new SellerOutboundShipmentTrackingEventDto(
                Status: e.NormalizedStatus.Id,
                Description: e.CarrierStatusDesc,
                OccurredAt: e.EventTime))
            .ToList();

        return new SellerOutboundShipmentDto(
            ShipmentId: shipment.Id.Value,
            OrderId: shipment.OrderId.Value,
            OrderNumber: order?.OrderNumber.Value ?? string.Empty,
            Status: shipment.Status.Id,
            ProviderCode: shipment.ProviderCode?.Id,
            ProviderDisplayName: shipment.ProviderCode?.Id, // v1: use code as display name
            CarrierTrackingNumber: shipment.CarrierTrackingNumber,
            ShipmentMode: shipment.ShipmentMode.Id,
            CreatedAt: shipment.CreatedAt,
            ModifiedAt: shipment.ModifiedAt,
            PackedAt: shipment.PackedAt,
            DispatchedAt: shipment.DispatchedAt,
            DeliveredAt: shipment.DeliveredAt,
            Item: itemDto,
            Recipient: recipientDto,
            TrackingEvents: trackingEvents);
    }
}
