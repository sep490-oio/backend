using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.OrderContext.DTOs;
using OIO.Application.Context.OrderContext.Mappings;
using OIO.Application.Context.OrderContext.Services;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.OrderContext.Errors;
using OIO.Domain.Context.OrderContext.ValueObjects.Ids;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseItems;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;
using OIO.Application.Context.UserContext.Services;

namespace OIO.Application.Context.OrderContext.Commands.SellerOrderProgression;

/// <summary>
/// Seller (self-ship only) marks a Processing order as picked up.
/// Transitions Processing → PickedUp.
/// </summary>
public sealed record MarkOrderPickedUpCommand(Guid OrderId) : ICommand<OrderDto>, IHasValidate
{
    public ViolationsError Validate() =>
        MarkOrderPickedUpCommand.Check()
            .WithOwnerName("MarkOrderPickedUp")
            .Field(OrderId).NotEmptyGuid();
}

/// <summary>
/// Seller (self-ship only) marks a PickedUp order as on delivering.
/// Transitions PickedUp → OnDelivering.
/// </summary>
public sealed record MarkOrderOnDeliveringCommand(Guid OrderId) : ICommand<OrderDto>, IHasValidate
{
    public ViolationsError Validate() =>
        MarkOrderOnDeliveringCommand.Check()
            .WithOwnerName("MarkOrderOnDelivering")
            .Field(OrderId).NotEmptyGuid();
}

/// <summary>
/// Seller (self-ship only) marks an OnDelivering order as delivered.
/// Transitions OnDelivering → Delivered and starts the decision window.
/// </summary>
public sealed record MarkOrderDeliveredCommand(Guid OrderId) : ICommand<OrderDto>, IHasValidate
{
    public ViolationsError Validate() =>
        MarkOrderDeliveredCommand.Check()
            .WithOwnerName("MarkOrderDelivered")
            .Field(OrderId).NotEmptyGuid();
}

/// <summary>
/// Shared helper: loads the order, enforces seller ownership, and verifies
/// the order is on the seller_self_ship flow (no WarehouseItem linked to
/// the underlying auction item). Warehouse-managed orders must be advanced
/// only by outbound-shipment event handlers, never by seller commands.
/// </summary>
internal static class SellerOrderProgressionGuard
{
    public static async Task<Result<Order, Error>> LoadSelfShipOrderAsync(
        IDbContext dbContext,
        ICurrentUser currentUser,
        Guid orderIdValue,
        CancellationToken cancellationToken)
    {
        var orderId = OrderId.From(orderIdValue);
        var order = await dbContext.Set<Order>()
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

        if (order is null)
            return OrderErrors.Order.NotFound(orderId);

        if (order.SellerId != currentUser.UserId)
            return Error.Forbidden(
                "Order.ProgressionForbidden",
                "Only the seller can progress this order.");

        // seller_self_ship ⇔ the order's auction item has NEVER entered the
        // platform warehouse. The presence of ANY WarehouseItem row (even
        // a fully-dispatched one) proves warehouse ownership — once the
        // platform touched the item, the seller must not progress the
        // order by hand. This mirrors the tightened flow-detection rule
        // used by OrderMappings.BuildSellerFulfillment.
        var auction = await dbContext.Set<Auction>()
            .AsNoTracking()
            .Include(a => a.Item)
            .FirstOrDefaultAsync(a => a.Id == order.AuctionId, cancellationToken);

        if (auction?.Item is not null)
        {
            var itemGuid = auction.Item.Id.Value;
            var warehouseItem = await dbContext.Set<WarehouseItem>()
                .AsNoTracking()
                .Where(wi => wi.ItemId == itemGuid)
                .FirstOrDefaultAsync(cancellationToken);

            if (warehouseItem is not null)
                return Error.Conflict(
                    "Order.NotSelfShip",
                    "This order is warehouse-managed; seller cannot update its progression directly.");
        }

        return order;
    }
}

internal sealed class MarkOrderPickedUpCommandHandler(
    IDbContext dbContext,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IClock clock)
    : ICommandHandler<MarkOrderPickedUpCommand, OrderDto>
{
    public async Task<Result<OrderDto, Error>> Handle(
        MarkOrderPickedUpCommand request,
        CancellationToken cancellationToken)
    {
        var loaded = await SellerOrderProgressionGuard.LoadSelfShipOrderAsync(
            dbContext, currentUser, request.OrderId, cancellationToken);
        if (loaded.IsFailure) return loaded.Error;

        var order = loaded.Value;
        var result = order.MarkPickedUp(clock.UtcNow);
        if (result.IsFailure) return result.Error;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return order.ToDto();
    }
}

internal sealed class MarkOrderOnDeliveringCommandHandler(
    IDbContext dbContext,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IClock clock)
    : ICommandHandler<MarkOrderOnDeliveringCommand, OrderDto>
{
    public async Task<Result<OrderDto, Error>> Handle(
        MarkOrderOnDeliveringCommand request,
        CancellationToken cancellationToken)
    {
        var loaded = await SellerOrderProgressionGuard.LoadSelfShipOrderAsync(
            dbContext, currentUser, request.OrderId, cancellationToken);
        if (loaded.IsFailure) return loaded.Error;

        var order = loaded.Value;
        var result = order.MarkOnDelivering(clock.UtcNow);
        if (result.IsFailure) return result.Error;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return order.ToDto();
    }
}

internal sealed class MarkOrderDeliveredCommandHandler(
    IDbContext dbContext,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IOrderDeliveryService orderDeliveryService,
    IClock clock)
    : ICommandHandler<MarkOrderDeliveredCommand, OrderDto>
{
    public async Task<Result<OrderDto, Error>> Handle(
        MarkOrderDeliveredCommand request,
        CancellationToken cancellationToken)
    {
        var loaded = await SellerOrderProgressionGuard.LoadSelfShipOrderAsync(
            dbContext, currentUser, request.OrderId, cancellationToken);
        if (loaded.IsFailure) return loaded.Error;

        // Delegate the Delivered transition + return-decision window math to
        // the shared IOrderDeliveryService so self-ship, warehouse-outbound,
        // and buyer proof-of-delivery flows stay in lockstep.
        var result = await orderDeliveryService.MarkAsDeliveredAsync(loaded.Value, clock.UtcNow, cancellationToken);
        if (result.IsFailure) return result.Error;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return result.Value;
    }
}
