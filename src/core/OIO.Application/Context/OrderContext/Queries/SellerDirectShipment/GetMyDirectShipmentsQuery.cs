using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.OrderContext.DTOs;
using OIO.Application.Context.UserContext.Services;
using OIO.Application.Extensions;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.OrderContext.Enums;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.SeedWork.Errors;
using ShipmentAggregate = OIO.Domain.Context.OrderContext.Aggregates.SellerDirectShipments.SellerDirectShipment;

namespace OIO.Application.Context.OrderContext.Queries.SellerDirectShipment;

/// <summary>
/// Buyer-scoped paged list of direct shipments. Returns shipments whose
/// parent order belongs to the current buyer, enriched with product +
/// recipient + seller display name + decision-window context so the buyer
/// list page can gate action CTAs without loading the order.
/// </summary>
public sealed record GetMyDirectShipmentsQuery(PagedParameters Parameters, string? Status = null)
    : IQuery<PagedList<MyDirectShipmentListItemDto>>;

internal sealed class GetMyDirectShipmentsQueryHandler(
    IDbContext dbContext,
    ICurrentUser currentUser)
    : IQueryHandler<GetMyDirectShipmentsQuery, PagedList<MyDirectShipmentListItemDto>>
{
    public async Task<Result<PagedList<MyDirectShipmentListItemDto>, Error>> Handle(
        GetMyDirectShipmentsQuery request,
        CancellationToken cancellationToken)
    {
        var parameters = request.Parameters;

        var buyerOrderIds = await dbContext.Set<Order>()
            .AsNoTracking()
            .Where(o => o.BuyerId == currentUser.UserId)
            .Select(o => o.Id)
            .ToListAsync(cancellationToken);

        if (buyerOrderIds.Count == 0)
        {
            return new List<MyDirectShipmentListItemDto>().ToPagedList(0, parameters);
        }

        var shipmentsQuery = dbContext.Set<ShipmentAggregate>()
            .AsNoTracking()
            .Where(s => buyerOrderIds.Contains(s.OrderId));

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

        var sellerIds = orders.Select(o => o.SellerId).Distinct().ToList();
        var sellers = await dbContext.Set<User>()
            .AsNoTracking()
            .Include(u => u.SellerProfile)
            .Where(u => sellerIds.Contains(u.Id))
            .ToListAsync(cancellationToken);
        var sellersById = sellers.ToDictionary(u => u.Id);

        var nowUtc = DateTime.UtcNow;
        var deliveredStatus = SellerDirectShipmentStatus.Delivered.Id;

        var dtos = pagedShipments
            .Select(s =>
            {
                ordersById.TryGetValue(s.OrderId, out var order);
                var (itemDto, recipientDto) = ShipmentListItemMapping.BuildContext(order, auctionsById);

                string? sellerDisplayName = null;
                if (order is not null && sellersById.TryGetValue(order.SellerId, out var seller))
                {
                    sellerDisplayName = ResolveSellerDisplayName(seller);
                }

                var decisionEndsAt = order?.DecisionWindowEndsAt;
                var decisionWindowOpen = decisionEndsAt is not null && decisionEndsAt.Value > nowUtc;
                var isDelivered = s.Status.Id == deliveredStatus;
                var notCompleted = order is not null && order.Status != OrderStatus.Completed;
                var notDisputed = s.DisputedAt is null;

                var canSubmitProofOfDelivery =
                    isDelivered && s.BuyerReceivedPackageAt is null && decisionWindowOpen;

                var canAccept =
                    isDelivered && notCompleted && notDisputed && decisionWindowOpen;

                var canDispute = canAccept;

                return new MyDirectShipmentListItemDto(
                    ShipmentId: s.Id.Value,
                    ShipmentIdDisplay: s.ShipmentIdDisplay,
                    OrderId: s.OrderId.Value,
                    OrderNumber: order?.OrderNumber.Value ?? string.Empty,
                    InternalTrackingCode: s.InternalTrackingCode,
                    ExternalCarrierName: s.ExternalCarrierName,
                    ExternalTrackingCode: s.ExternalTrackingCode,
                    Status: s.Status.Id,
                    CreatedAt: s.CreatedAt,
                    SellerDeclaredShippedAt: s.SellerDeclaredShippedAt,
                    DeliveredAt: s.DeliveredAt,
                    BuyerReceivedPackageAt: s.BuyerReceivedPackageAt,
                    BuyerAcceptedAt: s.BuyerAcceptedAt,
                    ManualReviewRequired: s.ManualReviewRequired,
                    Item: itemDto,
                    Recipient: recipientDto,
                    SellerDisplayName: sellerDisplayName,
                    DecisionWindowEndsAt: decisionEndsAt,
                    CanSubmitProofOfDelivery: canSubmitProofOfDelivery,
                    CanAccept: canAccept,
                    CanDispute: canDispute);
            })
            .ToList();

        return dtos.ToPagedList(totalCount, parameters);
    }

    private static string? ResolveSellerDisplayName(User? user)
    {
        if (user is null) return null;
        if (user.SellerProfile is not null && !string.IsNullOrWhiteSpace(user.SellerProfile.StoreName))
            return user.SellerProfile.StoreName;
        var profile = user.Profile;
        if (profile?.Name is not null)
        {
            if (!string.IsNullOrWhiteSpace(profile.Name.DisplayName)) return profile.Name.DisplayName;
            if (!string.IsNullOrWhiteSpace(profile.Name.FullName)) return profile.Name.FullName;
        }
        return user.UserName?.Value;
    }
}
