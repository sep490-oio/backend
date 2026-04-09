using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.NotificationContext;
using OIO.Application.Context.NotificationContext.Commands.CreateNotification;
using OIO.Application.Context.OrderContext.Services;
using OIO.Domain.Context.NotificationContext.Enums;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.OrderContext.ValueObjects.Ids;
using OIO.Domain.Context.WarehouseContext.Aggregates.OutboundShipments.Events;

namespace OIO.Application.Context.OrderContext.EventHandlers;

internal sealed class OrderMarkedShippedEventHandler(
    IDbContext dbContext,
    IUnitOfWork unitOfWork,
    IClock clock,
    ISender sender,
    ILogger<OrderMarkedShippedEventHandler> logger)
    : INotificationHandler<OutboundShipmentPickedUpEvent>
{
    public async Task Handle(OutboundShipmentPickedUpEvent notification, CancellationToken cancellationToken)
    {
        var order = await dbContext.Set<Order>()
            .FirstOrDefaultAsync(x => x.Id == OrderId.From(Guid.Parse(notification.OrderId)), cancellationToken);

        if (order is null)
            return;

        // Warehouse-managed orders advance via outbound events. Pickup by
        // the carrier maps to Order.PickedUp. InTransit/Delivered events
        // continue through the new progression (OnDelivering → Delivered).
        // If the order is already past Processing (e.g. race with a
        // manual seller action), MarkPickedUp returns a failure which we
        // swallow to keep the event handler idempotent.
        var result = order.MarkPickedUp(clock.UtcNow);
        if (result.IsFailure)
            return;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Notify Buyer
        await NotificationDispatch.DispatchAsync(
            sender,
            logger,
            new CreateNotificationCommand(
                UserId: order.BuyerId.Value,
                NotificationType: "order",
                EventType: "order_shipped",
                Title: "Don hang dang tren duong giao",
                Message: $"Don hang {order.OrderNumber.Value} da bat dau duoc giao.",
                Priority: NotificationPriority.High,
                EntityType: "Order",
                EntityId: order.Id.Value),
            cancellationToken);

        // Notify Seller
        await NotificationDispatch.DispatchAsync(
            sender,
            logger,
            new CreateNotificationCommand(
                UserId: order.SellerId.Value,
                NotificationType: "order",
                EventType: "order_shipped",
                Title: "Don hang da duoc gui",
                Message: $"Don hang {order.OrderNumber.Value} da duoc don vi van chuyen tiep nhan.",
                Priority: NotificationPriority.Normal,
                EntityType: "Order",
                EntityId: order.Id.Value),
            cancellationToken);
    }
}

internal sealed class OrderMarkedOnDeliveringEventHandler(
    IDbContext dbContext,
    IUnitOfWork unitOfWork,
    IClock clock,
    ISender sender,
    ILogger<OrderMarkedOnDeliveringEventHandler> logger)
    : INotificationHandler<OutboundShipmentInTransitEvent>
{
    public async Task Handle(OutboundShipmentInTransitEvent notification, CancellationToken cancellationToken)
    {
        var order = await dbContext.Set<Order>()
            .FirstOrDefaultAsync(x => x.Id == OrderId.From(Guid.Parse(notification.OrderId)), cancellationToken);

        if (order is null)
            return;

        // Warehouse-managed orders advance from PickedUp → OnDelivering when
        // the carrier reports InTransit. Idempotent via the aggregate guard.
        var result = order.MarkOnDelivering(clock.UtcNow);
        if (result.IsFailure)
            return;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await NotificationDispatch.DispatchAsync(
            sender,
            logger,
            new CreateNotificationCommand(
                UserId: order.BuyerId.Value,
                NotificationType: "order",
                EventType: "order_on_delivering",
                Title: "Don hang dang tren duong giao",
                Message: $"Don hang {order.OrderNumber.Value} da len duong giao den ban.",
                Priority: NotificationPriority.Normal,
                EntityType: "Order",
                EntityId: order.Id.Value),
            cancellationToken);
    }
}

internal sealed class OrderMarkedDeliveredEventHandler(
    IDbContext dbContext,
    IUnitOfWork unitOfWork,
    IClock clock,
    IRuntimeSettings runtimeSettings,
    IOrderDeliveryService orderDeliveryService,
    ISender sender,
    ILogger<OrderMarkedDeliveredEventHandler> logger)
    : INotificationHandler<OutboundShipmentDeliveredEvent>
{
    public async Task Handle(OutboundShipmentDeliveredEvent notification, CancellationToken cancellationToken)
    {
        var order = await dbContext.Set<Order>()
            .FirstOrDefaultAsync(x => x.Id == OrderId.From(Guid.Parse(notification.OrderId)), cancellationToken);

        if (order is null)
            return;

        var decisionWindowDays = runtimeSettings.Order.ReturnDecisionWindowDays;

        // Route through IOrderDeliveryService so the warehouse (platform_managed)
        // delivery path mirrors the seller direct-shipment delivered path. The
        // service is idempotent and keeps buyer-protection window logic in one place.
        var deliveryResult = await orderDeliveryService.MarkAsDeliveredAsync(order, clock.UtcNow, cancellationToken);
        if (deliveryResult.IsFailure)
            return;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Notify Buyer
        await NotificationDispatch.DispatchAsync(
            sender,
            logger,
            new CreateNotificationCommand(
                UserId: order.BuyerId.Value,
                NotificationType: "order",
                EventType: "order_delivered",
                Title: "Giao hang thanh cong",
                Message: $"Don hang {order.OrderNumber.Value} da giao thanh cong. Ban co {decisionWindowDays} ngay de khieu nai.",
                Priority: NotificationPriority.High,
                EntityType: "Order",
                EntityId: order.Id.Value),
            cancellationToken);

        // Notify Seller
        await NotificationDispatch.DispatchAsync(
            sender,
            logger,
            new CreateNotificationCommand(
                UserId: order.SellerId.Value,
                NotificationType: "order",
                EventType: "order_delivered",
                Title: "Yeu cau xac nhan nhan hang",
                Message: $"Khach hang da nhan duoc don {order.OrderNumber.Value}. Tien se thanh toan sau thoi gian cho.",
                Priority: NotificationPriority.Normal,
                EntityType: "Order",
                EntityId: order.Id.Value),
            cancellationToken);
    }
}

internal sealed class OrderMarkedFailedEventHandler(
    IDbContext dbContext,
    IUnitOfWork unitOfWork,
    IClock clock,
    ISender sender,
    ILogger<OrderMarkedFailedEventHandler> logger)
    : INotificationHandler<OutboundShipmentFailedEvent>
{
    public async Task Handle(OutboundShipmentFailedEvent notification, CancellationToken cancellationToken)
    {
        var order = await dbContext.Set<Order>()
            .FirstOrDefaultAsync(x => x.Id == OrderId.From(Guid.Parse(notification.OrderId)), cancellationToken);

        if (order is null)
            return;

        // Auto-dispute for safety
        var result = order.MarkAsDisputed(clock.UtcNow);
        if (result.IsFailure)
            return;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await NotificationDispatch.DispatchAsync(
            sender,
            logger,
            new CreateNotificationCommand(
                UserId: order.BuyerId.Value,
                NotificationType: "order",
                EventType: "order_failed",
                Title: "Su co giao hang",
                Message: $"Don hang {order.OrderNumber.Value} gap su co: {notification.Reason}.",
                Priority: NotificationPriority.High,
                EntityType: "Order",
                EntityId: order.Id.Value),
            cancellationToken);
    }
}

internal sealed class OrderMarkedReturningEventHandler(
    ISender sender,
    IDbContext dbContext,
    ILogger<OrderMarkedReturningEventHandler> logger)
    : INotificationHandler<OutboundShipmentReturningEvent>
{
    public async Task Handle(OutboundShipmentReturningEvent notification, CancellationToken cancellationToken)
    {
        var order = await dbContext.Set<Order>()
            .FirstOrDefaultAsync(x => x.Id == OrderId.From(Guid.Parse(notification.OrderId)), cancellationToken);

        if (order is null)
            return;

        await NotificationDispatch.DispatchAsync(
            sender,
            logger,
            new CreateNotificationCommand(
                UserId: order.BuyerId.Value,
                NotificationType: "order",
                EventType: "order_returning",
                Title: "Don hang dang quay dau",
                Message: $"Don hang {order.OrderNumber.Value} dang duoc tra lai: {notification.Reason}.",
                Priority: NotificationPriority.High,
                EntityType: "Order",
                EntityId: order.Id.Value),
            cancellationToken);
    }
}

