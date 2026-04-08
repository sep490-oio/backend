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
using OIO.Domain.SeedWork.Errors;
using ShipmentAggregate = OIO.Domain.Context.OrderContext.Aggregates.SellerDirectShipments.SellerDirectShipment;

namespace OIO.Application.Context.OrderContext.Queries.SellerDirectShipment;

/// <summary>
/// Seller-scoped paged list of direct shipments. Returns shipments whose
/// parent order belongs to the current seller, enriched with the product
/// + recipient snapshot so the seller list page can render cards without
/// round-tripping orders/auctions.
/// </summary>
public sealed record GetSellerDirectShipmentsQuery(PagedParameters Parameters, string? Status = null)
    : IQuery<PagedList<SellerDirectShipmentListItemDto>>;

internal sealed class GetSellerDirectShipmentsQueryHandler(
    IDbContext dbContext,
    ICurrentUser currentUser)
    : IQueryHandler<GetSellerDirectShipmentsQuery, PagedList<SellerDirectShipmentListItemDto>>
{
    public async Task<Result<PagedList<SellerDirectShipmentListItemDto>, Error>> Handle(
        GetSellerDirectShipmentsQuery request,
        CancellationToken cancellationToken)
    {
        var parameters = request.Parameters;

        var sellerOrderIds = await dbContext.Set<Order>()
            .AsNoTracking()
            .Where(o => o.SellerId == currentUser.UserId)
            .Select(o => o.Id)
            .ToListAsync(cancellationToken);

        if (sellerOrderIds.Count == 0)
        {
            return new List<SellerDirectShipmentListItemDto>().ToPagedList(0, parameters);
        }

        var shipmentsQuery = dbContext.Set<ShipmentAggregate>()
            .AsNoTracking()
            .Where(s => sellerOrderIds.Contains(s.OrderId));

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            var statusFilter = request.Status.Trim();
            shipmentsQuery = shipmentsQuery.Where(s => s.Status.Id == statusFilter);
        }

        var totalCount = await shipmentsQuery.CountAsync(cancellationToken);

        var pagedShipments = await shipmentsQuery
            .OrderByDescending(s => s.CreatedAt)
            .Page(parameters)
            .ToListAsync(cancellationToken);

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
            .Select(s => ShipmentListItemMapping.BuildSellerDto(s, ordersById, auctionsById))
            .ToList();

        return dtos.ToPagedList(totalCount, parameters);
    }
}

/// <summary>
/// Shared mapping helper for seller + buyer direct shipment list projections.
/// </summary>
internal static class ShipmentListItemMapping
{
    public static SellerDirectShipmentListItemDto BuildSellerDto(
        ShipmentAggregate shipment,
        IReadOnlyDictionary<OrderId, Order> ordersById,
        IReadOnlyDictionary<AuctionId, Auction> auctionsById)
    {
        ordersById.TryGetValue(shipment.OrderId, out var order);
        var (itemDto, recipientDto) = BuildContext(order, auctionsById);

        return new SellerDirectShipmentListItemDto(
            ShipmentId: shipment.Id.Value,
            ShipmentIdDisplay: shipment.ShipmentIdDisplay,
            OrderId: shipment.OrderId.Value,
            OrderNumber: order?.OrderNumber.Value ?? string.Empty,
            InternalTrackingCode: shipment.InternalTrackingCode,
            ExternalCarrierName: shipment.ExternalCarrierName,
            ExternalTrackingCode: shipment.ExternalTrackingCode,
            Status: shipment.Status.Id,
            CreatedAt: shipment.CreatedAt,
            SellerDeclaredShippedAt: shipment.SellerDeclaredShippedAt,
            DeliveredAt: shipment.DeliveredAt,
            BuyerReceivedPackageAt: shipment.BuyerReceivedPackageAt,
            BuyerAcceptedAt: shipment.BuyerAcceptedAt,
            ManualReviewRequired: shipment.ManualReviewRequired,
            Item: itemDto,
            Recipient: recipientDto);
    }

    public static (ShipmentListItemItemDto? Item, ShipmentListItemRecipientDto? Recipient) BuildContext(
        Order? order,
        IReadOnlyDictionary<AuctionId, Auction> auctionsById)
    {
        if (order is null) return (null, null);

        ShipmentListItemItemDto? itemDto = null;
        if (auctionsById.TryGetValue(order.AuctionId, out var auction) && auction.Item is not null)
        {
            var primaryImageUrl = auction.Item.Media
                .Where(m => m.IsPrimary)
                .OrderBy(m => m.SortOrder)
                .Select(m => m.Info.SecureUrl)
                .FirstOrDefault();

            itemDto = new ShipmentListItemItemDto(
                ItemId: auction.Item.Id.Value,
                ItemTitle: auction.Item.Title.Value,
                PrimaryImageUrl: primaryImageUrl,
                FinalPrice: order.Pricing.ItemPrice.Amount,
                Currency: order.Currency);
        }

        ShipmentListItemRecipientDto? recipientDto = null;
        if (order.Shipping is not null)
        {
            recipientDto = new ShipmentListItemRecipientDto(
                RecipientName: order.Shipping.RecipientName,
                PhoneNumber: order.Shipping.Phone,
                ComposedAddress: order.Shipping.Address);
        }

        return (itemDto, recipientDto);
    }
}
