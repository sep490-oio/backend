using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.WarehouseContext.DTOs;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.OrderContext.ValueObjects.Ids;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseItems;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.WarehouseContext.Queries.GetWarehouseStaffOutboundOrder;

/// <summary>
/// Single-order detail for the warehouse-staff outbound queue.
/// Returns the same row shape as <c>GetWarehouseStaffOutboundQueueQuery</c> so the
/// FE detail page can render without re-paging the entire queue.
/// </summary>
public sealed record GetWarehouseStaffOutboundOrderQuery(Guid OrderId)
    : IQuery<WarehouseStaffOutboundQueueItemDto>;

internal sealed class GetWarehouseStaffOutboundOrderQueryHandler(IDbContext dbContext)
    : IQueryHandler<GetWarehouseStaffOutboundOrderQuery, WarehouseStaffOutboundQueueItemDto>
{
    public async Task<Result<WarehouseStaffOutboundQueueItemDto, Error>> Handle(
        GetWarehouseStaffOutboundOrderQuery request,
        CancellationToken cancellationToken)
    {
        var orderId = OrderId.From(request.OrderId);

        var order = await dbContext.Set<Order>()
            .Include(o => o.OutboundShipments)
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

        if (order is null)
        {
            return Error.NotFound("Order.NotFound", $"Order {request.OrderId} not found.");
        }

        var auction = await dbContext.Set<Auction>()
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

        Guid warehouseItemId = Guid.Empty;
        if (item is not null)
        {
            var itemGuid = item.Id.Value;
            var wi = await dbContext.Set<WarehouseItem>()
                .AsNoTracking()
                .Where(w => w.ItemId == itemGuid)
                .OrderByDescending(w => w.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);
            if (wi is not null)
            {
                warehouseItemId = wi.Id.Value;
            }
        }

        var seller = await dbContext.Set<User>()
            .AsNoTracking()
            .Include(u => u.SellerProfile)
            .FirstOrDefaultAsync(u => u.Id == order.SellerId, cancellationToken);

        var sellerDisplayName = ResolveSellerDisplayName(seller);

        string? shippingSummary = null;
        if (order.Shipping is not null)
        {
            shippingSummary = order.Shipping.IsStructured
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

        return new WarehouseStaffOutboundQueueItemDto(
            OrderId: order.Id.Value,
            OrderNumber: order.OrderNumber.Value,
            OrderStatus: order.Status.Id,
            OrderPaidAt: order.PaidAt,
            AuctionId: order.AuctionId.Value,
            WarehouseItemId: warehouseItemId,
            ItemTitle: item?.Title.Value,
            ItemPrimaryImageUrl: primaryImageUrl,
            BuyerRecipientName: order.Shipping?.RecipientName,
            BuyerShippingAddress: shippingSummary,
            SellerDisplayName: sellerDisplayName);
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
