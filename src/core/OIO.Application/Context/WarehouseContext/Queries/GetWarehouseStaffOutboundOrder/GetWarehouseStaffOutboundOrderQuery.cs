using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.WarehouseContext.DTOs;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.OrderContext.Enums;
using OIO.Domain.Context.OrderContext.ValueObjects.Ids;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.WarehouseContext.Aggregates.ShippingProviders;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseItems;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseStorage;
using OIO.Domain.Context.WarehouseContext.Enums;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.WarehouseContext.Queries.GetWarehouseStaffOutboundOrder;

/// <summary>
/// Single-order detail for the warehouse-staff outbound booking screen. Returns
/// a <see cref="WarehouseStaffOutboundOrderDetailDto"/> with everything the
/// booking form needs (recipient address, package defaults, item price,
/// storage location, etc) so the FE doesn't have to re-join the queue endpoint.
/// </summary>
public sealed record GetWarehouseStaffOutboundOrderQuery(Guid OrderId)
    : IQuery<WarehouseStaffOutboundOrderDetailDto>;

internal sealed class GetWarehouseStaffOutboundOrderQueryHandler(IDbContext dbContext)
    : IQueryHandler<GetWarehouseStaffOutboundOrderQuery, WarehouseStaffOutboundOrderDetailDto>
{
    public async Task<Result<WarehouseStaffOutboundOrderDetailDto, Error>> Handle(
        GetWarehouseStaffOutboundOrderQuery request,
        CancellationToken cancellationToken)
    {
        var orderId = OrderId.From(request.OrderId);

        // Load-then-batch-enrich: never traverse VO-id boundaries inside a single
        // EF query — separate queries are cheaper than the inevitable translation
        // failures we've hit in earlier passes.
        var order = await dbContext.Set<Order>()
            .Include(o => o.OutboundShipments)
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

        if (order is null)
            return Error.NotFound("Order.NotFound", $"Order {request.OrderId} not found.");

        // Gate on Processing — the queue only surfaces these rows; the detail
        // page must refuse anything else so we don't half-render a stale order.
        if (order.Status != OrderStatus.Processing)
            return Error.NotFound("Order.NotActionable", $"Order {request.OrderId} is not in a state that accepts outbound booking.");

        // Refuse if there's already an active outbound shipment (same filter
        // as the queue handler — keep the two consistent).
        var hasActiveOutbound = order.OutboundShipments.Any(s =>
            s.Status != OutboundShipmentStatus.Cancelled &&
            s.Status != OutboundShipmentStatus.Failed &&
            s.Status != OutboundShipmentStatus.Returned &&
            s.Status != OutboundShipmentStatus.Delivered);
        if (hasActiveOutbound)
            return Error.NotFound("Order.AlreadyHasOutbound", $"Order {request.OrderId} already has an active outbound shipment.");

        var auction = await dbContext.Set<Auction>()
            .AsNoTracking()
            .Include(a => a.Item)
                .ThenInclude(i => i.Media)
            .FirstOrDefaultAsync(a => a.Id == order.AuctionId, cancellationToken);

        var item = auction?.Item;
        if (item is null)
            return Error.NotFound("Order.ItemMissing", $"Item for order {request.OrderId} could not be resolved.");

        var primaryImageUrl = item.Media
            .Where(m => m.IsPrimary)
            .OrderBy(m => m.SortOrder)
            .Select(m => m.Info.SecureUrl)
            .FirstOrDefault();

        // Warehouse item — pick the most recent shippable row.
        var itemGuid = item.Id.Value;
        var shippable = new[]
        {
            WarehouseItemStatus.Stored,
            WarehouseItemStatus.Received,
            WarehouseItemStatus.Inspected,
        };
        var warehouseItem = await dbContext.Set<WarehouseItem>()
            .AsNoTracking()
            .Where(w => w.ItemId == itemGuid && shippable.Contains(w.Status))
            .OrderByDescending(w => w.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (warehouseItem is null)
            return Error.NotFound("Order.NotWarehouseManaged", $"Order {request.OrderId} has no shippable warehouse item.");

        string? storageLocationLabel = null;
        if (warehouseItem.StorageLocationId is not null)
        {
            var loc = await dbContext.Set<WarehouseStorageLocation>()
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.Id == warehouseItem.StorageLocationId, cancellationToken);
            storageLocationLabel = loc?.Label;
        }

        var seller = await dbContext.Set<User>()
            .AsNoTracking()
            .Include(u => u.SellerProfile)
            .FirstOrDefaultAsync(u => u.Id == order.SellerId, cancellationToken);
        var sellerDisplayName = ResolveSellerDisplayName(seller);

        // Compose address for display; still surface structured parts below so
        // the FE form can prefill individual inputs.
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

        var itemPriceDefault = order.Pricing?.ItemPrice.Amount ?? 0m;

        // Best-effort default provider lookup — used to label the platform-managed
        // shipment-mode option on the FE. If no default is configured, both
        // strings remain null and the FE falls back to "GHN (platform default)".
        var defaultProvider = await dbContext.Set<ShippingProviderConfig>()
            .AsNoTracking()
            .Where(c => c.IsDefault && c.IsActive)
            .Select(c => new { Code = c.ProviderCode.Id, c.DisplayName })
            .FirstOrDefaultAsync(cancellationToken);

        return new WarehouseStaffOutboundOrderDetailDto(
            OrderId: order.Id.Value,
            OrderNumber: order.OrderNumber.Value,
            OrderStatus: order.Status.Id,
            OrderPaidAt: order.PaidAt,
            WarehouseItemId: warehouseItem.Id.Value,
            WarehouseItemStatus: warehouseItem.Status.Id,
            StorageLocationLabel: storageLocationLabel,
            ItemTitle: item.Title.Value,
            ItemPrimaryImageUrl: primaryImageUrl,
            ItemPriceDefault: itemPriceDefault,
            RecipientName: order.Shipping?.RecipientName,
            RecipientPhone: order.Shipping?.Phone,
            Street: order.Shipping?.Street,
            Ward: order.Shipping?.Ward,
            District: order.Shipping?.District,
            Province: order.Shipping?.City,
            PostalCode: order.Shipping?.PostalCode,
            ComposedAddress: composedAddress,
            SellerDisplayName: sellerDisplayName,
            // Package defaults: no ShippingProviderConfig box-size defaults exist,
            // so use a conservative 1kg / 20x15x10cm carton — staff can override.
            WeightGrams: 1000,
            LengthCm: 20,
            WidthCm: 15,
            HeightCm: 10,
            InsuranceValueDefault: itemPriceDefault,
            // Order is already paid so COD default is zero.
            CodAmountDefault: 0m,
            DefaultProviderCode: defaultProvider?.Code,
            DefaultProviderLabel: defaultProvider?.DisplayName);
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
