using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.OrderContext.DTOs;
using OIO.Application.Context.OrderContext.Mappings;
using OIO.Application.Context.OrderContext.Services;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.OrderContext.Errors;
using OIO.Domain.Context.OrderContext.ValueObjects.Ids;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseItems;
using OIO.Domain.SeedWork.Errors;
using ShipmentAggregate = OIO.Domain.Context.OrderContext.Aggregates.SellerDirectShipments.SellerDirectShipment;

namespace OIO.Application.Context.OrderContext.Commands.SellerDirectShipment;

/// <summary>
/// Seller creates the (single) direct shipment record for a self-ship order.
/// Returns Conflict if a row already exists for the order.
/// The resulting DTO carries <c>QrPayload</c> (canonical payload/deep-link string to encode
/// into a QR image client-side) and <c>QrCodeUrl</c> (legacy/optional — DO NOT assume this is
/// an image URL; FE must render QR from <c>QrPayload</c>).
/// </summary>
public sealed record CreateSellerDirectShipmentCommand(Guid OrderId)
    : ICommand<SellerDirectShipmentDto>;

internal sealed class CreateSellerDirectShipmentCommandHandler(
    IDbContext dbContext,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IClock clock,
    IAppInfo appInfo,
    ISellerDirectShipmentTokenService tokenService)
    : ICommandHandler<CreateSellerDirectShipmentCommand, SellerDirectShipmentDto>
{
    public async Task<Result<SellerDirectShipmentDto, Error>> Handle(
        CreateSellerDirectShipmentCommand request,
        CancellationToken cancellationToken)
    {
        var orderIdValue = OrderId.From(request.OrderId);
        var order = await dbContext.Set<Order>()
            .FirstOrDefaultAsync(o => o.Id == orderIdValue, cancellationToken);

        if (order is null)
            return OrderErrors.Order.NotFound(orderIdValue);

        // Authz: only the seller of this order can create the direct shipment.
        // Mismatches → 404 to prevent existence enumeration.
        if (order.SellerId != currentUser.UserId)
            return OrderErrors.Order.NotFound(orderIdValue);

        // Re-derive seller_self_ship: an order is self-ship iff the underlying
        // auction item has NEVER been registered in the platform warehouse.
        // Mirrors OrderMappings.BuildSellerFulfillment.
        var auction = await dbContext.Set<Auction>()
            .AsNoTracking()
            .Include(a => a.Item)
            .FirstOrDefaultAsync(a => a.Id == order.AuctionId, cancellationToken);

        if (auction?.Item is not null)
        {
            var itemGuid = auction.Item.Id.Value;
            var hasWarehouseItem = await dbContext.Set<WarehouseItem>()
                .AsNoTracking()
                .AnyAsync(wi => wi.ItemId == itemGuid, cancellationToken);

            if (hasWarehouseItem)
                return Error.Conflict(
                    "SellerDirectShipment.NotSelfShip",
                    "This order is warehouse-managed; a seller-direct shipment cannot be created.");
        }

        var existing = await dbContext.Set<ShipmentAggregate>()
            .AsNoTracking()
            .AnyAsync(s => s.OrderId == orderIdValue, cancellationToken);

        if (existing)
            return Error.Conflict(
                "SellerDirectShipment.AlreadyExists",
                "A direct shipment already exists for this order.");

        var now = clock.UtcNow;
        var todayStart = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, DateTimeKind.Utc);
        var perDayCount = await dbContext.Set<ShipmentAggregate>()
            .AsNoTracking()
            .CountAsync(s => s.CreatedAt >= todayStart, cancellationToken);

        var displayId = $"DSH-{now:yyyyMMdd}-{(perDayCount + 1):D4}";
        var internalTrackingCode = $"ITR-{Guid.NewGuid():N}".Substring(0, 16).ToUpperInvariant();

        var shipmentId = Guid.CreateVersion7();

        // Issue the first QR token (version 1) and build an absolute deep-link
        // URL embedding it. Both QrPayload and legacy QrCodeUrl point at the
        // canonical buyer-facing /me/shipments/{id}/receive?token=... route.
        const int initialVersion = 1;
        var token = tokenService.Issue(
            SellerDirectShipmentId.From(shipmentId),
            order.Id.Value,
            order.BuyerId.Value,
            initialVersion,
            now);

        var feBase = (appInfo.FeUrl ?? string.Empty).TrimEnd('/');
        var qrCodeUrl = $"{feBase}/me/shipments/{shipmentId}/receive?token={token}";
        var qrPayload = qrCodeUrl;

        var shipment = ShipmentAggregate.CreateWithId(
            id: shipmentId,
            orderId: order.Id.Value,
            shipmentIdDisplay: displayId,
            internalTrackingCode: internalTrackingCode,
            qrPayload: qrPayload,
            qrCodeUrl: qrCodeUrl,
            nowUtc: now);

        shipment.RecordQrTokenIssued(initialVersion, now, now);

        dbContext.Insert(shipment);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return shipment.ToDto();
    }
}
