using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.OrderContext.DTOs;
using OIO.Application.Context.OrderContext.Queries.GetSellerOutboundShipments;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.OrderContext.ValueObjects.Ids;
using OIO.Domain.Context.WarehouseContext.Aggregates.OutboundShipments;
using OIO.Domain.Context.WarehouseContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.OrderContext.Queries.GetSellerOutboundShipmentById;

/// <summary>
/// Seller-safe detail read for a single outbound shipment. Returns the rich
/// seller DTO (product + recipient + order context) — intentionally different
/// from the warehouse-generic OutboundShipmentDto so seller pages never
/// reference warehouse-permissioned payloads.
///
/// Route: GET /api/me/orders/seller-direct-ship/outbound-shipments/{shipmentId}
/// </summary>
public sealed record GetSellerOutboundShipmentByIdQuery(Guid ShipmentId)
    : IQuery<SellerOutboundShipmentDto>;

internal sealed class GetSellerOutboundShipmentByIdQueryHandler(
    IDbContext dbContext,
    ICurrentUser currentUser)
    : IQueryHandler<GetSellerOutboundShipmentByIdQuery, SellerOutboundShipmentDto>
{
    public async Task<Result<SellerOutboundShipmentDto, Error>> Handle(
        GetSellerOutboundShipmentByIdQuery request,
        CancellationToken cancellationToken)
    {
        var shipmentId = OutboundShipmentId.From(request.ShipmentId);

        var shipment = await dbContext.Set<OutboundShipment>()
            .AsNoTracking()
            .Include(s => s.TrackingEvents)
            .FirstOrDefaultAsync(s => s.Id == shipmentId, cancellationToken);

        if (shipment is null)
            return Error.NotFound("OutboundShipment.NotFound", "Outbound shipment was not found.");

        var order = await dbContext.Set<Order>()
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == shipment.OrderId, cancellationToken);

        if (order is null || order.SellerId != currentUser.UserId)
            return Error.Forbidden("OutboundShipment.Forbidden", "You are not allowed to view this shipment.");

        // Load the related auction + item so the detail page renders full product context.
        var auction = await dbContext.Set<Auction>()
            .AsNoTracking()
            .Include(a => a.Item)
                .ThenInclude(i => i.Media)
            .FirstOrDefaultAsync(a => a.Id == order.AuctionId, cancellationToken);

        var ordersById = new Dictionary<OrderId, Order> { [order.Id] = order };
        var auctionsById = auction is null
            ? new Dictionary<AuctionId, Auction>()
            : new Dictionary<AuctionId, Auction> { [auction.Id] = auction };

        return SellerOutboundShipmentMapping.BuildDto(shipment, ordersById, auctionsById);
    }
}
