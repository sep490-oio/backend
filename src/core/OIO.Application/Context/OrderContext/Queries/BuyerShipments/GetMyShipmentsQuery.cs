using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.OrderContext.DTOs;
using OIO.Application.Context.UserContext.Services;
using OIO.Application.Context.WarehouseContext.Queries.GetBuyerOutboundShipmentByToken;
using OIO.Application.Extensions;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.OrderContext.ValueObjects.Ids;
using OIO.Domain.Context.OrderContext.Enums;
using OIO.Domain.Context.WarehouseContext.Aggregates.OutboundShipments;
using OIO.Domain.SeedWork.Errors;
using DirectShipmentAggregate = OIO.Domain.Context.OrderContext.Aggregates.SellerDirectShipments.SellerDirectShipment;

namespace OIO.Application.Context.OrderContext.Queries.BuyerShipments;

/// <summary>
/// Buyer-safe unified shipment feed. Merges the caller's
/// <c>SellerDirectShipment</c> rows (for self-ship orders) with
/// warehouse-booked <c>OutboundShipment</c> rows into a single paged list
/// keyed on <see cref="BuyerShipmentListItemDto.ShipmentKind"/>. Action flags
/// are precomputed so the buyer list UI can gate CTAs without a detail round-trip.
/// </summary>
/// <param name="Parameters">Standard paging.</param>
/// <param name="Status">Optional raw shipment-status filter (post-merge).</param>
/// <param name="ShipmentKind">Optional kind filter: <c>seller_direct</c> | <c>warehouse_outbound</c>.</param>
/// <param name="Search">Optional case-insensitive substring match on order number / tracking / item title.</param>
public sealed record GetMyShipmentsQuery(
    PagedParameters Parameters,
    string? Status = null,
    string? ShipmentKind = null,
    string? Search = null)
    : IQuery<PagedList<BuyerShipmentListItemDto>>;

internal sealed class GetMyShipmentsQueryHandler(
    IDbContext dbContext,
    ICurrentUser currentUser)
    : IQueryHandler<GetMyShipmentsQuery, PagedList<BuyerShipmentListItemDto>>
{
    public const string KindSellerDirect = "seller_direct";
    public const string KindWarehouseOutbound = "warehouse_outbound";

    public async Task<Result<PagedList<BuyerShipmentListItemDto>, Error>> Handle(
        GetMyShipmentsQuery request,
        CancellationToken cancellationToken)
    {
        var parameters = request.Parameters;
        var kind = request.ShipmentKind?.Trim().ToLowerInvariant();
        var status = request.Status?.Trim();
        var search = request.Search?.Trim();

        var buyerOrderIds = await dbContext.Set<Order>()
            .AsNoTracking()
            .Where(o => o.BuyerId == currentUser.UserId)
            .Select(o => o.Id)
            .ToListAsync(cancellationToken);

        if (buyerOrderIds.Count == 0)
        {
            return new List<BuyerShipmentListItemDto>().ToPagedList(0, parameters);
        }

        var includeDirect = kind is null || kind == KindSellerDirect;
        var includeOutbound = kind is null || kind == KindWarehouseOutbound;

        var directShipments = includeDirect
            ? await dbContext.Set<DirectShipmentAggregate>()
                .AsNoTracking()
                .Where(s => buyerOrderIds.Contains(s.OrderId))
                .ToListAsync(cancellationToken)
            : new List<DirectShipmentAggregate>();

        var outboundShipments = includeOutbound
            ? await dbContext.Set<OutboundShipment>()
                .AsNoTracking()
                .Where(s => buyerOrderIds.Contains(s.OrderId))
                .ToListAsync(cancellationToken)
            : new List<OutboundShipment>();

        var orderIds = directShipments.Select(s => s.OrderId)
            .Concat(outboundShipments.Select(s => s.OrderId))
            .Distinct()
            .ToList();

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

        var merged = new List<BuyerShipmentListItemDto>(directShipments.Count + outboundShipments.Count);

        var nowUtc = DateTime.UtcNow;
        var deliveredDirectStatus = SellerDirectShipmentStatus.Delivered.Id;

        foreach (var s in directShipments)
        {
            ordersById.TryGetValue(s.OrderId, out var order);
            var (itemDto, _) = ShipmentListItemMappingBridge.BuildContext(order, auctionsById);

            var decisionEndsAt = order?.DecisionWindowEndsAt;
            var decisionWindowOpen = decisionEndsAt is not null && decisionEndsAt.Value > nowUtc;
            var isDelivered = s.Status.Id == deliveredDirectStatus;
            var notCompleted = order is not null && order.Status != OrderStatus.Completed;
            var notDisputed = s.DisputedAt is null;

            var canAcknowledgeReceived =
                isDelivered && s.BuyerReceivedPackageAt is null && decisionWindowOpen;

            var canAccept = isDelivered && notCompleted && notDisputed && decisionWindowOpen;
            var canDispute = canAccept;
            var canSubmitProof = isDelivered;

            merged.Add(new BuyerShipmentListItemDto(
                ShipmentKind: KindSellerDirect,
                ShipmentId: s.Id.Value,
                OrderId: s.OrderId.Value,
                OrderNumber: order?.OrderNumber.Value ?? string.Empty,
                Status: s.Status.Id,
                ItemTitle: itemDto?.ItemTitle,
                ItemImageUrl: itemDto?.PrimaryImageUrl,
                CarrierName: s.ExternalCarrierName,
                CarrierTrackingNumber: s.ExternalTrackingCode,
                InternalTrackingCode: s.InternalTrackingCode,
                DecisionWindowEndsAt: decisionEndsAt,
                CanAcknowledgeReceived: canAcknowledgeReceived,
                CanAccept: canAccept,
                CanDispute: canDispute,
                HasActiveDispute: s.DisputedAt is not null,
                CreatedAt: s.CreatedAt,
                UpdatedAt: s.ModifiedAt ?? s.CreatedAt,
                QrAvailable: !string.IsNullOrEmpty(s.QrPayload) && s.QrTokenRevokedAt is null,
                CanSubmitProof: canSubmitProof));
        }

        foreach (var s in outboundShipments)
        {
            if (!ordersById.TryGetValue(s.OrderId, out var order) || order is null) continue;

            // Reuse the shared builder for per-row canX / dispute / qr logic.
            // Cost is small because the pre-pagination count is bounded by
            // the caller's own shipment history.
            var detail = await BuyerOutboundShipmentDetailBuilder.BuildAsync(dbContext, s, order, cancellationToken);

            merged.Add(new BuyerShipmentListItemDto(
                ShipmentKind: KindWarehouseOutbound,
                ShipmentId: detail.ShipmentId,
                OrderId: detail.OrderId,
                OrderNumber: detail.OrderNumber,
                Status: detail.Status,
                ItemTitle: detail.ItemTitle,
                ItemImageUrl: detail.ItemPrimaryImageUrl,
                CarrierName: detail.ExternalCarrierName,
                CarrierTrackingNumber: detail.CarrierTrackingNumber,
                InternalTrackingCode: detail.ClientOrderCode,
                DecisionWindowEndsAt: detail.DecisionWindowEndsAt,
                CanAcknowledgeReceived: false,
                CanAccept: false,
                CanDispute: false,
                HasActiveDispute: detail.HasActiveDispute,
                CreatedAt: s.CreatedAt,
                UpdatedAt: s.ModifiedAt ?? s.CreatedAt,
                QrAvailable: detail.QrAvailable,
                CanSubmitProof: false));
        }

        IEnumerable<BuyerShipmentListItemDto> filtered = merged;

        if (!string.IsNullOrWhiteSpace(status))
            filtered = filtered.Where(x => string.Equals(x.Status, status, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(search))
        {
            var needle = search.ToLowerInvariant();
            filtered = filtered.Where(x =>
                (x.OrderNumber?.ToLowerInvariant().Contains(needle) ?? false) ||
                (x.ItemTitle?.ToLowerInvariant().Contains(needle) ?? false) ||
                (x.CarrierTrackingNumber?.ToLowerInvariant().Contains(needle) ?? false) ||
                (x.InternalTrackingCode?.ToLowerInvariant().Contains(needle) ?? false));
        }

        var ordered = filtered
            .OrderByDescending(x => x.UpdatedAt)
            .ToList();

        var totalCount = ordered.Count;
        var pageNumber = parameters.PageNumber ?? 1;
        var pageSize = parameters.PageSize ?? 10;
        var paged = ordered
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return paged.ToPagedList(totalCount, parameters);
    }
}

/// <summary>
/// Thin bridge to the internal <c>ShipmentListItemMapping.BuildContext</c>
/// helper that lives alongside the seller direct-shipment query. Re-declared
/// here because the original is file-internal; this keeps the projection
/// logic in one place without exposing the seller helper cross-feature.
/// </summary>
internal static class ShipmentListItemMappingBridge
{
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
