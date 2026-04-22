using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Data;
using OIO.Application.Context.NotificationContext;
using OIO.Application.Context.NotificationContext.Commands.CreateNotification;
using OIO.Domain.Context.NotificationContext.Enums;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.OrderContext.Aggregates.Orders.Events;
using OIO.Domain.Context.OrderContext.ValueObjects.Ids;

namespace OIO.Application.Context.OrderContext.EventHandlers;

/// <summary>
/// V1: status-flip only. Notifies both parties that the return window expired.
/// Escrow revocation is deferred to V2 (plan M2 / ADR follow-up #1) — this
/// handler intentionally does NOT call any IEscrowService method.
/// </summary>
internal sealed class OrderReturnExpiredEventHandler(
    ISender sender,
    IDbContext dbContext,
    ILogger<OrderReturnExpiredEventHandler> logger)
    : INotificationHandler<OrderReturnExpiredEvent>
{
    public async Task Handle(OrderReturnExpiredEvent notification, CancellationToken cancellationToken)
    {
        logger.LogWarning(
            "OrderReturn {OrderReturnId} expired — escrow revocation deferred to V2 (see ADR follow-up #1). Order={OrderId}, Reason={Reason}.",
            notification.OrderReturnId,
            notification.OrderId,
            notification.Reason);

        var order = await dbContext.Set<Order>()
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == OrderId.From(notification.OrderId), cancellationToken);

        if (order is null)
        {
            logger.LogWarning(
                "OrderReturnExpiredEventHandler: Order {OrderId} not found.",
                notification.OrderId);
            return;
        }

        // Notify buyer.
        await NotificationDispatch.DispatchAsync(
            sender,
            logger,
            new CreateNotificationCommand(
                UserId:           notification.BuyerId,
                NotificationType: "order",
                EventType:        "order_return_expired",
                Title:            "Cua so tra hang da het han",
                Message:          $"Cua so tra hang cho don {order.OrderNumber.Value} da het han. Do ban khong gui hang ve, yeu cau tra hang duoc dong lai.",
                Priority:         NotificationPriority.High,
                EntityType:       "OrderReturn",
                EntityId:         notification.OrderReturnId),
            cancellationToken);

        // Notify seller.
        await NotificationDispatch.DispatchAsync(
            sender,
            logger,
            new CreateNotificationCommand(
                UserId:           notification.SellerId,
                NotificationType: "order",
                EventType:        "order_return_expired",
                Title:            "Cua so tra hang da het han",
                Message:          $"Cua so tra hang cho don {order.OrderNumber.Value} da het han do nguoi mua khong gui hang ve.",
                Priority:         NotificationPriority.Normal,
                EntityType:       "OrderReturn",
                EntityId:         notification.OrderReturnId),
            cancellationToken);
    }
}
