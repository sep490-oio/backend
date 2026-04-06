using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Data;
using OIO.Application.Context.NotificationContext.Commands.CreateNotification;
using OIO.Domain.Context.NotificationContext.Enums;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.OrderContext.Aggregates.Orders.Events;
using OIO.Domain.Context.OrderContext.ValueObjects.Ids;
using OIO.Domain.Context.WarehouseContext.Aggregates.OutboundShipments.Events;

namespace OIO.Application.Context.NotificationContext.EventHandlers;

internal sealed class OrderCancelledNotificationHandler
    : INotificationHandler<OrderCancelledEvent>
{
    private readonly ISender _sender;
    private readonly ILogger<OrderCancelledNotificationHandler> _logger;

    public OrderCancelledNotificationHandler(
        ISender sender,
        ILogger<OrderCancelledNotificationHandler> logger)
    {
        _sender = sender;
        _logger = logger;
    }

    public async Task Handle(OrderCancelledEvent notification, CancellationToken cancellationToken)
    {
        await NotificationDispatch.DispatchAsync(
            _sender,
            _logger,
            new CreateNotificationCommand(
                UserId: Guid.Parse(notification.BuyerId),
                NotificationType: "order",
                EventType: "order_cancelled",
                Title: "Don hang da bi huy",
                Message: $"Don hang {notification.OrderNumber} da bi huy. Ly do: {notification.Reason}",
                Priority: NotificationPriority.High,
                EntityType: "Order",
                EntityId: Guid.Parse(notification.OrderId),
                Metadata: NotificationDispatch.SerializeMetadata(new
                {
                    orderId = notification.OrderId,
                    orderNumber = notification.OrderNumber,
                    reason = notification.Reason
                })),
            cancellationToken);
    }
}

internal sealed class OrderShippedNotificationHandler
    : INotificationHandler<OutboundShipmentPickedUpEvent>
{
    private readonly IDbContext _dbContext;
    private readonly ISender _sender;
    private readonly ILogger<OrderShippedNotificationHandler> _logger;

    public OrderShippedNotificationHandler(
        IDbContext dbContext,
        ISender sender,
        ILogger<OrderShippedNotificationHandler> logger)
    {
        _dbContext = dbContext;
        _sender = sender;
        _logger = logger;
    }

    public async Task Handle(OutboundShipmentPickedUpEvent notification, CancellationToken cancellationToken)
    {
        var orderId = OrderId.From(Guid.Parse(notification.OrderId));
        var order = await _dbContext.GetByIdAsync<Order, OrderId>(
            orderId,
            queryBuilder: query => query.AsNoTracking(),
            cancellationToken: cancellationToken);

        if (order is null)
        {
            _logger.LogWarning(
                "Skipped shipped notification because order {OrderId} was not found.",
                notification.OrderId);
            return;
        }

        await NotificationDispatch.DispatchAsync(
            _sender,
            _logger,
            new CreateNotificationCommand(
                UserId: order.BuyerId.Value,
                NotificationType: "order",
                EventType: "order_shipped",
                Title: "Don hang da duoc giao don vi van chuyen",
                Message:
                    $"Don hang {order.OrderNumber.Value} da duoc ban giao cho don vi van chuyen. " +
                    $"Ma van don: {notification.CarrierTrackingNumber}.",
                Priority: NotificationPriority.Normal,
                EntityType: "Order",
                EntityId: order.Id.Value,
                Metadata: NotificationDispatch.SerializeMetadata(new
                {
                    orderId = order.Id.Value,
                    orderNumber = order.OrderNumber.Value,
                    outboundShipmentId = notification.OutboundShipmentId,
                    trackingNumber = notification.CarrierTrackingNumber,
                    providerCode = notification.ProviderCode
                })),
            cancellationToken);
    }
}

internal sealed class OrderDeliveredNotificationHandler
    : INotificationHandler<OutboundShipmentDeliveredEvent>
{
    private readonly IDbContext _dbContext;
    private readonly ISender _sender;
    private readonly ILogger<OrderDeliveredNotificationHandler> _logger;

    public OrderDeliveredNotificationHandler(
        IDbContext dbContext,
        ISender sender,
        ILogger<OrderDeliveredNotificationHandler> logger)
    {
        _dbContext = dbContext;
        _sender = sender;
        _logger = logger;
    }

    public async Task Handle(OutboundShipmentDeliveredEvent notification, CancellationToken cancellationToken)
    {
        var orderId = OrderId.From(Guid.Parse(notification.OrderId));
        var order = await _dbContext.GetByIdAsync<Order, OrderId>(
            orderId,
            queryBuilder: query => query.AsNoTracking(),
            cancellationToken: cancellationToken);

        if (order is null)
        {
            _logger.LogWarning(
                "Skipped delivered notification because order {OrderId} was not found.",
                notification.OrderId);
            return;
        }

        await NotificationDispatch.DispatchAsync(
            _sender,
            _logger,
            new CreateNotificationCommand(
                UserId: order.BuyerId.Value,
                NotificationType: "order",
                EventType: "order_delivered",
                Title: "Don hang da duoc giao thanh cong",
                Message:
                    $"Don hang {order.OrderNumber.Value} da duoc giao thanh cong vao " +
                    $"{notification.DeliveredAt:dd/MM/yyyy HH:mm}.",
                Priority: NotificationPriority.Normal,
                EntityType: "Order",
                EntityId: order.Id.Value,
                Metadata: NotificationDispatch.SerializeMetadata(new
                {
                    orderId = order.Id.Value,
                    orderNumber = order.OrderNumber.Value,
                    outboundShipmentId = notification.OutboundShipmentId,
                    providerCode = notification.ProviderCode,
                    deliveredAt = notification.DeliveredAt
                })),
            cancellationToken);

        if (order.DecisionWindowEndsAt.HasValue)
        {
            await NotificationDispatch.DispatchAsync(
                _sender,
                _logger,
                new CreateNotificationCommand(
                    UserId: order.BuyerId.Value,
                    NotificationType: "order",
                    EventType: "decision_window_started",
                    Title: "Thoi gian quyet dinh da bat dau",
                    Message:
                        $"Ban co the yeu cau tra hang cho don {order.OrderNumber.Value} truoc " +
                        $"{order.DecisionWindowEndsAt.Value:dd/MM/yyyy HH:mm}.",
                    Priority: NotificationPriority.Normal,
                    EntityType: "Order",
                    EntityId: order.Id.Value,
                    Metadata: NotificationDispatch.SerializeMetadata(new
                    {
                        orderId = order.Id.Value,
                        orderNumber = order.OrderNumber.Value,
                        decisionWindowEndsAt = order.DecisionWindowEndsAt.Value
                    })),
                cancellationToken);
        }
    }
}

internal sealed class OrderCompletedNotificationHandler
    : INotificationHandler<OrderCompletedEvent>
{
    private readonly ISender _sender;
    private readonly ILogger<OrderCompletedNotificationHandler> _logger;

    public OrderCompletedNotificationHandler(
        ISender sender,
        ILogger<OrderCompletedNotificationHandler> logger)
    {
        _sender = sender;
        _logger = logger;
    }

    public async Task Handle(OrderCompletedEvent notification, CancellationToken cancellationToken)
    {
        // To Buyer
        await NotificationDispatch.DispatchAsync(
            _sender,
            _logger,
            new CreateNotificationCommand(
                UserId: Guid.Parse(notification.BuyerId),
                NotificationType: "order",
                EventType: "order_completed",
                Title: "Don hang da hoan tat",
                Message: $"Don hang {notification.OrderNumber} da duoc hoan tat. Cam on ban da mua xam!",
                Priority: NotificationPriority.Normal,
                EntityType: "Order",
                EntityId: Guid.Parse(notification.OrderId),
                Metadata: NotificationDispatch.SerializeMetadata(new
                {
                    orderId = notification.OrderId,
                    orderNumber = notification.OrderNumber,
                    completedAt = notification.CompletedAt
                })),
            cancellationToken);

        // To Seller
        await NotificationDispatch.DispatchAsync(
            _sender,
            _logger,
            new CreateNotificationCommand(
                UserId: Guid.Parse(notification.SellerId),
                NotificationType: "order",
                EventType: "escrow_released",
                Title: "Tien ban hang da duoc chuyen vao vi",
                Message: $"Don hang {notification.OrderNumber} da hoan tat, tien da duoc cong vao vi cua ban.",
                Priority: NotificationPriority.High,
                EntityType: "Order",
                EntityId: Guid.Parse(notification.OrderId),
                Metadata: NotificationDispatch.SerializeMetadata(new
                {
                    orderId = notification.OrderId,
                    orderNumber = notification.OrderNumber,
                    completedAt = notification.CompletedAt
                })),
            cancellationToken);
    }
}
